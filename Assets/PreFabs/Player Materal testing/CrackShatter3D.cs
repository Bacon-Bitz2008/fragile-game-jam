using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Progressive cracks + shatter for a 3D character (MeshRenderer or SkinnedMeshRenderer).
/// Put on the player, assign the renderer that uses Custom/URP/CrackLit3D.
/// Call RegisterDodge() (or TakeHit(worldPoint)) from your own code.
///
/// How it works: at startup the script clones the renderer's mesh and bakes two things into it,
/// the vertex position normalised to the model's size (UV3) and a "growth order" (UV4).
/// The shader reads those, so it never depends on object-space scale or skinning quirks.
/// REQUIREMENT: Read/Write must be enabled on the model's import settings.
///
/// Shatter modes:
///  - preFracturedPrefab assigned: swaps to your pre-broken model.
///  - otherwise: cuts the mesh into chunks procedurally.
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

    [Header("Testing")]
    [Tooltip("Press T to register a dodge (crack). Press R to reset. Turn off when you hook up real dodging.")]
    [SerializeField] private bool enableTestKeys = true;

    public System.Action OnShattered;

    private static readonly int DamageId = Shader.PropertyToID("_Damage");

    private Material[] mats;
    private Mesh workMesh;                    // our private clone with crack data baked in
    private Vector3[] meshVerts;              // vertex positions in mesh space
    private List<Vector3> patternList;        // positions normalised to model size  -> UV3
    private List<Vector2> orderList;          // growth order, 0 at impact, 1 farthest -> UV4

    private int hits;
    private float damage;
    private bool shattered;
    private bool originSet;
    private bool ready;
    private Coroutine growRoutine;

    // ------------------------------------------------------------------ Setup

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        ready = PrepareMesh();
        mats = targetRenderer.materials;   // per-instance copies
        SetFloatAll(DamageId, 0f);
    }

    private void OnDestroy()
    {
        if (workMesh != null) Destroy(workMesh);
    }

    private bool PrepareMesh()
    {
        Mesh shared = GetSharedMesh();
        if (shared == null || !shared.isReadable)
        {
            Debug.LogError("CrackShatter3D: mesh missing or not readable. Select the model asset and enable Read/Write on its Model import tab.", this);
            return false;
        }

        // Private copy so other instances of the same model aren't affected
        workMesh = Instantiate(shared);
        workMesh.name = shared.name + " (Cracks)";
        if (targetRenderer is SkinnedMeshRenderer smr) smr.sharedMesh = workMesh;
        else targetRenderer.GetComponent<MeshFilter>().sharedMesh = workMesh;

        meshVerts = workMesh.vertices;
        int n = meshVerts.Length;

        Vector3 min = meshVerts[0], max = meshVerts[0];
        for (int i = 1; i < n; i++)
        {
            min = Vector3.Min(min, meshVerts[i]);
            max = Vector3.Max(max, meshVerts[i]);
        }
        Vector3 size = max - min;
        float maxExtent = Mathf.Max(size.x, size.y, size.z, 1e-5f);

        patternList = new List<Vector3>(n);
        orderList = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            patternList.Add((meshVerts[i] - min) / maxExtent);
            orderList.Add(new Vector2(1f, 0f));
        }
        workMesh.SetUVs(2, patternList);   // UV3
        workMesh.SetUVs(3, orderList);     // UV4
        return true;
    }

    /// <summary>Growth order = distance from the origin vertex, scaled so the farthest vertex is 1.</summary>
    private void SetCrackOrigin(int vertexIndex)
    {
        Vector3 o = patternList[vertexIndex];
        int n = patternList.Count;

        float maxD = 1e-5f;
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            d[i] = Vector3.Distance(patternList[i], o);
            if (d[i] > maxD) maxD = d[i];
        }
        for (int i = 0; i < n; i++) orderList[i] = new Vector2(d[i] / maxD, 0f);
        workMesh.SetUVs(3, orderList);
    }

    // ------------------------------------------------------------------ Testing

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

    /// <summary>Call this each time the player dodges. The crack starts at a random point on the body.</summary>
    /// 

    public void SetHealth(int currentHealth, int maxHealth)
    {
        if (!ready || shattered || maxHealth <= 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 100% health = 0 damage; 0% health = fully grown cracks.
        float targetDamage = 1f - ((float)currentHealth / maxHealth);

        // Keep the crack origin consistent as health decreases.
        if (!originSet)
        {
            originSet = true;

            int vi = Random.Range(0, meshVerts.Length);
            SetCrackOrigin(vi);
        }

        // Don't restart the animation unnecessarily.
        if (currentHealth > 0 && growRoutine != null)
            StopCoroutine(growRoutine);

        // Grow cracks toward the new health-based damage value.
        Vector3 impactPoint = targetRenderer.bounds.center;

        growRoutine = StartCoroutine(
            GrowCracks(targetDamage, currentHealth <= 0, impactPoint)
        );
    }

    public void RegisterDodge()
    {
        if (!ready) return;
        int vi = Random.Range(0, meshVerts.Length);
        Vector3 world = targetRenderer.transform.TransformPoint(meshVerts[vi]);
        Hit(vi, world);
    }

    /// <summary>Hit at a specific world point (e.g. where an attack landed).</summary>
    public void TakeHit(Vector3 worldImpact)
    {
        if (!ready) return;
        Vector3 local = targetRenderer.transform.InverseTransformPoint(worldImpact);
        int best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < meshVerts.Length; i++)
        {
            float d = (meshVerts[i] - local).sqrMagnitude;
            if (d < bestD) { bestD = d; best = i; }
        }
        Hit(best, worldImpact);
    }

    private void Hit(int originVertex, Vector3 worldPoint)
    {
        if (shattered) return;
        hits++;

        // Cracks spread from the FIRST impact. Moving the origin later would hide cracks already shown.
        if (!originSet)
        {
            originSet = true;
            SetCrackOrigin(originVertex);
        }

        float target = Mathf.Clamp01((float)hits / maxHits);
        if (growRoutine != null) StopCoroutine(growRoutine);
        growRoutine = StartCoroutine(GrowCracks(target, hits >= maxHits, worldPoint));

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
        if (mats == null) return;
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
        if (!ready) return;
        Transform t = targetRenderer.transform;

        // Positions/normals: the current animated pose for skinned meshes, otherwise the mesh itself.
        // Everything else (UVs, crack data, triangles) comes from workMesh; vertex order is identical.
        Mesh posSrc = workMesh;
        bool isTemp = false;
        if (targetRenderer is SkinnedMeshRenderer smr)
        {
            posSrc = new Mesh();
            smr.BakeMesh(posSrc);
            isTemp = true;
        }

        Vector3[] verts = posSrc.vertices;
        Vector3[] norms = posSrc.normals;
        Vector2[] uvs = workMesh.uv;
        bool hasN = norms.Length == verts.Length;
        bool hasUV = uvs.Length == verts.Length;

        Vector3 bmin = verts[0], bmax = verts[0];
        foreach (var v in verts) { bmin = Vector3.Min(bmin, v); bmax = Vector3.Max(bmax, v); }
        Vector3 bsize = bmax - bmin;
        Vector3 cell = new Vector3(
            Mathf.Max(bsize.x / shardGrid.x, 1e-5f),
            Mathf.Max(bsize.y / shardGrid.y, 1e-5f),
            Mathf.Max(bsize.z / shardGrid.z, 1e-5f));

        // 1) Group triangles into chunks by which grid cell their centre falls in (per submesh)
        var clusters = new Dictionary<(int, int, int, int), List<int>>();
        for (int sub = 0; sub < workMesh.subMeshCount; sub++)
        {
            int[] tris = workMesh.GetTriangles(sub);
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 c = (verts[tris[i]] + verts[tris[i + 1]] + verts[tris[i + 2]]) / 3f;
                Vector3 rel = c - bmin;
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
            int n = idx.Count;

            Vector3 center = Vector3.zero;
            foreach (int vi in idx) center += verts[vi];
            center /= n;

            var sv = new Vector3[n];
            var sn = new Vector3[n];
            var suv = new Vector2[n];
            var spat = new List<Vector3>(n);
            var sord = new List<Vector2>(n);
            var st = new int[n];
            for (int i = 0; i < n; i++)
            {
                int vi = idx[i];
                sv[i] = verts[vi] - center;          // recentre so the shard rotates about itself
                if (hasN) sn[i] = norms[vi];
                if (hasUV) suv[i] = uvs[vi];
                spat.Add(patternList[vi]);           // crack data travels with the shard,
                sord.Add(orderList[vi]);             // so the cracks stay exactly where they were
                st[i] = i;
            }

            var m = new Mesh { name = "Shard" };
            m.vertices = sv;
            if (hasUV) m.uv = suv;
            m.SetUVs(2, spat);
            m.SetUVs(3, sord);
            m.SetTriangles(st, 0);
            if (hasN) m.normals = sn; else m.RecalculateNormals();
            m.RecalculateBounds();

            var go = new GameObject("Shard");
            go.layer = layer >= 0 ? layer : 0;
            go.transform.SetPositionAndRotation(
                t.position + t.rotation * Vector3.Scale(center, scale), t.rotation);
            go.transform.localScale = scale;

            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mats[Mathf.Min(sub, mats.Length - 1)];   // already fully cracked

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

        if (isTemp) Destroy(posSrc);
    }

    // ------------------------------------------------------------------ Helpers

    private Mesh GetSharedMesh()
    {
        if (targetRenderer is SkinnedMeshRenderer smr) return smr.sharedMesh;
        var mf = targetRenderer.GetComponent<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
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
