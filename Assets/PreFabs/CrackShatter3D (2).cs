using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Progressive cracks + shatter for a 3D character.
/// Put on the player, assign the MeshRenderer or SkinnedMeshRenderer that uses Custom/URP/CrackLit3D.
/// Call RegisterDodge() (or TakeHit(worldPoint)) from your own code.
///
/// Shatter modes:
///  - preFracturedPrefab assigned: swaps to your pre-broken model (best quality).
///  - otherwise: cuts the current mesh into chunks procedurally (needs Read/Write enabled on the mesh).
/// </summary>
public class CrackShatter3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Cracks")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float crackGrowTime = 0.3f;
    [Tooltip("Pause after the last crack appears, before the player breaks apart.")]
    [SerializeField] private float shatterDelay = 0.25f;
    [SerializeField] private float hitStopTime = 0.05f;

    [Header("Shatter")]
    [Tooltip("Optional. A pre-fractured version of the model: children with Rigidbodies.")]
    [SerializeField] private GameObject preFracturedPrefab;
    [Tooltip("How many chunks the procedural shatter cuts the mesh into, per axis.")]
    [SerializeField] private Vector3Int shardGrid = new Vector3Int(4, 5, 4);
    [SerializeField] private float shardForce = 5f;
    [SerializeField] private float shardTorque = 4f;
    [SerializeField] private float shardLifetime = 2.5f;
    [Tooltip("Layer shards live on. Set it to collide with the ground but not the player.")]
    [SerializeField] private string shardLayerName = "Default";

    private static readonly int CrackColorId =
    Shader.PropertyToID("_CrackColor");

    private static readonly int CrackEmissionId =
        Shader.PropertyToID("_CrackEmission");


    public System.Action OnShattered;

    private static readonly int DamageId = Shader.PropertyToID("_Damage");
    private static readonly int ImpactId = Shader.PropertyToID("_ImpactPointOS");
    private static readonly int RadiusId = Shader.PropertyToID("_ImpactRadius");
    private static readonly int OffsetId = Shader.PropertyToID("_ObjectOffset");

    private Material[] mats;
    private int hits;
    private float damage;
    private bool shattered;
    private bool originSet;
    private Coroutine growRoutine;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError(
                "CrackShatter3D: No target Renderer assigned or found."
            );
            enabled = false;
            return;
        }

        // Create material instances for this object.
        mats = targetRenderer.materials;

        foreach (var m in mats)
        {
            if (m.HasProperty(CrackColorId))
                m.SetColor(CrackColorId, Color.white);

            if (m.HasProperty(CrackEmissionId))
                m.SetColor(
                    CrackEmissionId,
                    new Color(2f, 2f, 2f, 1f)
                );

            if (m.HasProperty(DamageId))
                m.SetFloat(DamageId, 0f);
        }
    }

    // ------------------------------------------------------------------ Testing

    [Header("Testing")]
    [Tooltip("Press T to register a dodge (crack). Press R to reset. Turn off when you hook up real dodging.")]
    [SerializeField] private bool enableTestKeys = true;

    private void Update()
    {
        if (!enableTestKeys) return;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.tKey.wasPressedThisFrame) RegisterDodge();
        if (kb.rKey.wasPressedThisFrame) ResetCracks();
#else
        if (Input.GetKeyDown(KeyCode.T)) RegisterDodge();
        if (Input.GetKeyDown(KeyCode.R)) ResetCracks();
#endif
    }

    // Right-click the component header in the Inspector (in Play mode) to use these
    [ContextMenu("Test: Register Dodge")]
    private void TestDodge() => RegisterDodge();

    [ContextMenu("Test: Reset Cracks")]
    private void TestReset() => ResetCracks();

    // ------------------------------------------------------------------ Hits

    /// <summary>Call this each time the player dodges. Impact point is a random spot on the body.</summary>
    public void RegisterDodge()
    {
        Bounds b = targetRenderer.bounds;
        Vector3 p = b.center + Vector3.Scale(Random.insideUnitSphere, b.extents) * 0.8f;
        TakeHit(p);
    }

    public void TakeHit(Vector3 worldImpact)
    {
        if (shattered) return;
        hits++;

        // Cracks spread from the FIRST impact. Moving the origin later would hide cracks already shown.
        if (!originSet)
        {
            originSet = true;
            Vector3 os = targetRenderer.transform.InverseTransformPoint(worldImpact);
            Mesh shared = GetSharedMesh();
            float radius = shared != null ? shared.bounds.size.magnitude : 2f;
            foreach (var m in mats)
            {
                if (!m.HasProperty(ImpactId)) continue;
                m.SetVector(ImpactId, os);
                m.SetFloat(RadiusId, radius);
            }
        }

        float target = Mathf.Clamp01((float)hits / maxHits);
        if (growRoutine != null) StopCoroutine(growRoutine);
        growRoutine = StartCoroutine(GrowCracks(target, hits >= maxHits, worldImpact));

        if (hitStopTime > 0f) StartCoroutine(HitStop());
        // Hook your camera shake here, scaled by `hits`.
    }

    private IEnumerator GrowCracks(float target, bool shatterAfter, Vector3 worldImpact)
    {
        float start = damage;
        float t = 0f;
        while (t < crackGrowTime)
        {
            t += Time.unscaledDeltaTime;
            damage = Mathf.Lerp(start, target, Mathf.Clamp01(t / crackGrowTime));
            SetFloatAll(DamageId, damage);
            yield return null;
        }
        damage = target;
        SetFloatAll(DamageId, damage);

        if (shatterAfter)
        {
            yield return new WaitForSecondsRealtime(shatterDelay);   // let the final cracks be seen
            Shatter(worldImpact);
        }
    }

    private IEnumerator HitStop()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopTime);
        Time.timeScale = 1f;
    }

    private void SetFloatAll(int id, float v)
    {
        foreach (var m in mats) if (m.HasProperty(id)) m.SetFloat(id, v);
    }

    // ------------------------------------------------------------------ Shatter

    public void Shatter(Vector3 worldImpact)
    {
        if (shattered) return;
        shattered = true;

        if (preFracturedPrefab != null) SpawnPrefractured(worldImpact);
        else SpawnProceduralShards(worldImpact);

        targetRenderer.enabled = false;
        OnShattered?.Invoke();   // disable input / colliders, trigger respawn or game over
    }

    private void SpawnPrefractured(Vector3 worldImpact)
    {
        Transform t = targetRenderer.transform;
        GameObject inst = Instantiate(preFracturedPrefab, t.position, t.rotation);
        inst.transform.localScale = t.lossyScale;

        foreach (var rb in inst.GetComponentsInChildren<Rigidbody>())
        {
            Vector3 dir = (rb.worldCenterOfMass - worldImpact).normalized + Vector3.up * 0.3f;
            rb.AddForce(dir.normalized * Random.Range(shardForce * 0.5f, shardForce), ForceMode.Impulse);
            rb.AddTorque(Random.onUnitSphere * shardTorque, ForceMode.Impulse);
            rb.gameObject.AddComponent<ShardLife>().Init(shardLifetime, null);
        }
        Destroy(inst, shardLifetime + 0.2f);
    }

    private void SpawnProceduralShards(Vector3 worldImpact)
    {
        Transform t = targetRenderer.transform;
        Mesh src = GetShatterMesh(out bool isTemp);
        if (src == null || !src.isReadable)
        {
            Debug.LogWarning("CrackShatter3D: mesh missing or not readable. Enable Read/Write on the model import settings.");
            if (isTemp && src != null) Destroy(src);
            return;
        }

        Vector3[] verts = src.vertices;
        Vector3[] norms = src.normals;
        Vector2[] uvs = src.uv;
        bool hasN = norms.Length == verts.Length;
        bool hasUV = uvs.Length == verts.Length;

        src.RecalculateBounds();
        Bounds b = src.bounds;
        Vector3 cell = new Vector3(
            Mathf.Max(b.size.x / shardGrid.x, 1e-5f),
            Mathf.Max(b.size.y / shardGrid.y, 1e-5f),
            Mathf.Max(b.size.z / shardGrid.z, 1e-5f));

        // 1) Group triangles into chunks by which grid cell their centre falls in (per submesh)
        var clusters = new Dictionary<(int, int, int, int), List<int>>();
        for (int sub = 0; sub < src.subMeshCount; sub++)
        {
            int[] tris = src.GetTriangles(sub);
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 c = (verts[tris[i]] + verts[tris[i + 1]] + verts[tris[i + 2]]) / 3f;
                Vector3 rel = c - b.min;
                int cx = Mathf.Clamp(Mathf.FloorToInt(rel.x / cell.x), 0, shardGrid.x - 1);
                int cy = Mathf.Clamp(Mathf.FloorToInt(rel.y / cell.y), 0, shardGrid.y - 1);
                int cz = Mathf.Clamp(Mathf.FloorToInt(rel.z / cell.z), 0, shardGrid.z - 1);

                var key = (sub, cx, cy, cz);
                if (!clusters.TryGetValue(key, out var list))
                    clusters[key] = list = new List<int>();
                list.Add(tris[i]); list.Add(tris[i + 1]); list.Add(tris[i + 2]);
            }
        }

        // 2) Build one mesh + rigidbody per chunk
        int layer = LayerMask.NameToLayer(shardLayerName);
        Vector3 scale = t.lossyScale;

        foreach (var kv in clusters)
        {
            int sub = kv.Key.Item1;
            List<int> idx = kv.Value;

            Vector3 center = Vector3.zero;
            foreach (int vi in idx) center += verts[vi];
            center /= idx.Count;

            int n = idx.Count;
            var sv = new Vector3[n];
            var sn = new Vector3[n];
            var suv = new Vector2[n];
            var st = new int[n];
            for (int i = 0; i < n; i++)
            {
                sv[i] = verts[idx[i]] - center;      // recentre so the shard rotates about itself
                if (hasN) sn[i] = norms[idx[i]];
                if (hasUV) suv[i] = uvs[idx[i]];
                st[i] = i;
            }

            var m = new Mesh { name = "Shard" };
            m.vertices = sv;
            if (hasUV) m.uv = suv;
            m.SetTriangles(st, 0);
            if (hasN) m.normals = sn; else m.RecalculateNormals();
            m.RecalculateBounds();

            var go = new GameObject("Shard");
            go.layer = layer >= 0 ? layer : 0;
            go.transform.SetPositionAndRotation(
                t.position + t.rotation * Vector3.Scale(center, scale), t.rotation);
            go.transform.localScale = scale;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mats[Mathf.Min(sub, mats.Length - 1)];   // already fully cracked

            // Offset keeps the crack pattern lined up after recentring
            var mpb = new MaterialPropertyBlock();
            mpb.SetVector(OffsetId, center);
            mr.SetPropertyBlock(mpb);

            var bc = go.AddComponent<BoxCollider>();
            bc.center = m.bounds.center;
            bc.size = Vector3.Max(m.bounds.size, Vector3.one * 0.03f);

            var rb = go.AddComponent<Rigidbody>();
            Vector3 dir = (go.transform.position - worldImpact).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Random.onUnitSphere;
            dir += Vector3.up * 0.3f;
            rb.AddForce(dir.normalized * Random.Range(shardForce * 0.5f, shardForce), ForceMode.Impulse);
            rb.AddTorque(Random.onUnitSphere * shardTorque, ForceMode.Impulse);

            go.AddComponent<ShardLife>().Init(shardLifetime, m);
        }

        if (isTemp) Destroy(src);
    }

    // ------------------------------------------------------------------ Helpers

    private Mesh GetSharedMesh()
    {
        if (targetRenderer is SkinnedMeshRenderer smr) return smr.sharedMesh;
        var mf = targetRenderer.GetComponent<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
    }

    private Mesh GetShatterMesh(out bool isTemp)
    {
        if (targetRenderer is SkinnedMeshRenderer smr)
        {
            var baked = new Mesh();
            smr.BakeMesh(baked);   // current animated pose
            isTemp = true;
            return baked;
        }
        isTemp = false;
        return GetSharedMesh();
    }

    /// <summary>Call on respawn to restore the player.</summary>
    public void ResetCracks()
    {
        shattered = false;
        originSet = false;
        hits = 0;
        damage = 0f;
        SetFloatAll(DamageId, 0f);
        targetRenderer.enabled = true;
    }
}

/// <summary>Shrinks a shard away over its final stretch of life, then destroys it.</summary>
public class ShardLife : MonoBehaviour
{
    private float life;
    private Mesh ownedMesh;

    public void Init(float lifetime, Mesh mesh)
    {
        life = lifetime;
        ownedMesh = mesh;
    }

    private IEnumerator Start()
    {
        Vector3 startScale = transform.localScale;
        float shrinkStart = life * 0.65f;
        float t = 0f;

        while (t < life)
        {
            t += Time.deltaTime;
            if (t > shrinkStart)
                transform.localScale = startScale * Mathf.Lerp(1f, 0f, (t - shrinkStart) / (life - shrinkStart));
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (ownedMesh != null) Destroy(ownedMesh);   // free the runtime-generated mesh
    }
}
