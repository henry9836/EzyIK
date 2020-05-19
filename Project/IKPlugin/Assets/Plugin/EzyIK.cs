using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HenryIK;
using UnityEngine.XR;

public class EzyIK : MonoBehaviour
{
    public IKPlugin.BoneStructure boneStructure;
    public IKPlugin.BoneStructure.MoveType movementType = IKPlugin.BoneStructure.MoveType.LINEAR;
    //How far to search for bones if below 0 then search till we cannot find any more objects
    public int maxDepthSearch = -1;

    [Header("Targetting")]
    public Transform target;
    public Transform bendTarget;
    
    [Header("Quailty Settings")]
    public int solverIterations = 5;
    public float solvedDistanceThreshold = 0.001f;

    [Header("Movement Settings")]
    public float nodeMoveSpeed = -1.0f;
    public float rotMoveSpeed = -1.0f;
    public AnimationCurve customMoveGraph;
    public AnimationCurve customRotGraph;

    [Header("Debugging")]
    [Range(0.0f, 5.0f)]
    public float visualiserScale = 0.3f;

    void Awake()
    {
        if (target)
        {
            GameObject me = this.gameObject;
            //BoneStructure(ref GameObject startBone, ref int maxDepth, ref Transform _target, ref Transform _bendTarget, ref float _arriveThreshold, ref int _maxSolveIterations, ref float _moveSpeed, ref float _rotSpeed, ref MoveType _moveType, ref AnimationCurve _moveCurve, ref AnimationCurve _rotCurve)

            boneStructure = new IKPlugin.BoneStructure(ref me, ref maxDepthSearch, ref target, ref bendTarget, ref solvedDistanceThreshold, ref solverIterations, ref nodeMoveSpeed, ref rotMoveSpeed, ref movementType, ref customMoveGraph, ref customRotGraph);
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
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(boneStructure.boneNodes[i].nodeTransform.position, visualiserScale);
                if (i > 0)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(boneStructure.boneNodes[i].nodeTransform.position, boneStructure.boneNodes[i - 1].nodeTransform.position);
                }
                if (boneStructure.bendTarget != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(boneStructure.bendTarget.position, visualiserScale);
                }
                if (boneStructure.target != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(boneStructure.target.position, visualiserScale);
                }
            }
        }
    }
}
