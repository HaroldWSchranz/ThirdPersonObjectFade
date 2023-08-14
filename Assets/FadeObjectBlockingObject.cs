using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeObjectBlockingObject : MonoBehaviour
{
    [SerializeField]
    private LayerMask LayerMask;
    [SerializeField]
    private Transform Target;
    [SerializeField]
    private Camera Camera;
    [SerializeField]
    [Range(0, 1f)]
    private float FadedAlpha = 0.25f;
    [SerializeField]
    private bool RetainShadows = true;
    [SerializeField]
    private Vector3 TargetPositionOffset = Vector3.up;
    [SerializeField]
    private float FadeSpeed = 1;

    [Header ("Read Only Data")]
    [SerializeField]
    private List<FadingObject> ObjectsBlockingView = new List<FadingObject> ();
    private Dictionary<FadingObject, Coroutine> RunningCoroutines = new Dictionary<FadingObject , Coroutine>();

    private RaycastHit[] Hits = new RaycastHit[10];

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CheckForObjects());
    }

    private IEnumerator CheckForObjects()
    {
        while (true)
        {
            int hits = Physics.RaycastNonAlloc()
                Camera.transform.position,
                (Target.transform.position + TargetPositionOffset - Camera.transform.position).normalized,
                Hits,
                Vector3.Distance(Camera.transform.position, Target.transform.position + TargetPositionOffset);
                LayerMask
            );

            if (hits > 0)
            {
                for(int i = 0; i < hits; i++)
                {
                    FadingObject fadingObject = GetFadingObjectFromHit(Hits[i]);

                    if (fadingObject != null && !ObjectsBlockingView.Contains(fadingObject))
                    {
                        if(RunningCoroutines.ContainsKey(fadingObject))
                        {
                            if (RunningCoroutines[fadingObject] != null)
                            {
                                StopCoroutine(RunningCoroutines[fadingObject]);
                            }

                            RunningCoroutines.Remove(fadingObject);
                        }

                        RunningCoroutines.Add(fadingObject, StartCoroutine(FadeObjectOut(fadingObject)));
                        ObjectsBlockingView.Add(fadingObject);
                    }
                }
            }

            FadeObjectsNoLongerBeingHit();

            ClearHits();

            yield return null;  
        }
    }

    private IEnumerator FadeObjectOut(FadingObject fadingObject)
    {
        foreach(Material material in FadingObject.Materials)
        {
            material.SetInt(" _SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt(" _DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt(" _ZWrite", 0);
            material.SetInt("_Surface", 1);

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("SHADOWCASTER", RetainShadows);

            material.SetOverrideTag("RenderType", "Transparent");

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        }

        float time = 0;

        while (FadingObject.Materials[0].color.a > FadedAlpha)
        {
            foreach (Material material in FadingObject.Materials)
            {
                if (material.HasProperty("_Color"))
                {
                    //Color color = material.GetColor("_Color");
                    material.color = new Color(
                        material.color.r,
                        material.color.g,
                        material.color.b,
                        Mathf.Lerp(FadingObject.InitialAlpha, FadedAlpha, time * FadeSpeed)
                    );
                }
            }

            time += Time.deltaTime;
            yield return null;
        }
        // https://www.youtube.com/watch?v=vmLIy62Gsnk
        if (RunningCoroutines.ContainsKey(FadingObject)) ///20230815020000 go bback YouTube to 14 minutes
    }

    private void ClearHits()
    {
        System.Array.Clear(Hits, 0, Hits.Length);
    }

    private FadingObject GetFadingObjectFromHit(RaycastHit Hit)
    {
        return Hit.collider != null ? Hit.collider.GetComponent<FadingObject>() : null;
    }
}
