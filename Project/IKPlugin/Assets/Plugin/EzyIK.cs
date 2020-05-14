using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HenryIK;
using UnityEngine.XR;

public class EzyIK : MonoBehaviour
{

    public List<IKPlugin.BoneNode> bones = new List<IKPlugin.BoneNode>();

    public Transform target;
    public Transform bendTarget;
    public bool useBendTarget = false;
    //How far to search for bones if below 0 then search till we cannot find any more objects
    public int maxDepthSearch = -1;

    [Range(0.1f, 5.0f)]
    public float visualiserScale = 0.5f;

    void Awake()
    {
        GameObject me = this.gameObject;
        bones = IKPlugin.Init(ref me, ref maxDepthSearch);
        me = null;
    }

    // Update is called once per frame
    void Update()
    {
        IKPlugin.IKStep(ref bones, ref target, ref bendTarget, ref useBendTarget);
    }

    //Draw In Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < bones.Count; i++)
        {
            Gizmos.DrawSphere(bones[i].nodeTransform.position, visualiserScale);
            if (i > 0)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(bones[i].nodeTransform.position, bones[i-1].nodeTransform.position);
                Gizmos.color = Color.green;
            }
        }
    }
}
