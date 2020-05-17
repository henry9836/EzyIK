using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HenryIK;
using UnityEngine.XR;

public class EzyIK : MonoBehaviour
{
    public IKPlugin.BoneStructure boneStructure;
    public Transform target;
    public Transform bendTarget;
    //How far to search for bones if below 0 then search till we cannot find any more objects
    public int maxDepthSearch = -1;
    public int solverIterations = 5;
    public float solvedDistanceThreshold = 0.001f;

    [Range(0.1f, 5.0f)]
    public float visualiserScale = 0.5f;

    void Awake()
    {
        if (target)
        {
            GameObject me = this.gameObject;
            boneStructure = new IKPlugin.BoneStructure(ref me, ref maxDepthSearch, ref target, ref bendTarget, ref solvedDistanceThreshold, ref solverIterations);
            me = null;
        }
        else
        {
            Debug.LogError($"No target set for {gameObject.name} cannot start IK System");
        }
    }

    // Update is called once per frame
    void Update()
    {
        IKPlugin.IKStep(ref boneStructure);
    }

    //Draw In Editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (boneStructure != null) {
            for (int i = 0; i < boneStructure.boneNodes.Count; i++)
            {
                Gizmos.DrawSphere(boneStructure.boneNodes[i].nodeTransform.position, visualiserScale);
                if (i > 0)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(boneStructure.boneNodes[i].nodeTransform.position, boneStructure.boneNodes[i - 1].nodeTransform.position);
                    Gizmos.color = Color.green;
                }
            }
        }
    }
}
