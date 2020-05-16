using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace HenryIK
{
    public class IKPlugin
    {
        public class BoneNode
        {
            public GameObject node;
            public Transform nodeTransform;
            public Transform parentTransform;
            public Vector3 startDir;
            public Quaternion startRot;
            public Vector3 startRotTarget;
            public float initalDistanceToChild = 0.0f;
            public bool isRootNode;
            public float totalChainDistance;
            public float totalChainDistanceSquared;

            //If we have left the bone section do not go any further upwards
            public BoneNode(ref GameObject _node)
            {
                node = _node;
                nodeTransform = node.transform;
            }
        }

        public class BoneStructure
        {
            public List<BoneNode> boneNodes = new List<BoneNode>();
            public Quaternion startTargetRot;
            public Transform rootNode;
            public Transform target;
            public float totalChainDistance;
            public float totalChainDistanceSquared;
            public float arriveThreshold;
            public int maxDepth = -1;
            public int maxSolveIterations = 5;

            //Create a bonestructure
            public BoneStructure(ref GameObject startBone, ref int maxDepth, ref Transform _target)
            {
                //Init here
                int currentDepth = 0;
                target = _target;
                Transform tmpRoot = startBone.transform;

                //While we haven't found the root node
                while (rootNode == null)
                {
                    //Append to our list of bone nodes
                    var tmpNode = tmpRoot.gameObject; 
                    boneNodes.Add(new BoneNode(ref tmpNode));

                    //Go up the chain until we find the root node or reach our max depth search
                    if (tmpRoot.parent == null || currentDepth >= maxDepth)
                    {
                        rootNode = tmpRoot;
                        break;
                    }
                    tmpRoot = tmpRoot.parent;
                    currentDepth++;
                }

                //Invert our list of bones for FABIK
                boneNodes.Reverse();

                //Get the stating target rotation
                startTargetRot = Quaternion.Inverse(target.rotation) * rootNode.rotation;

                //Set up extra infomation now that we now details about our root and target nodes
                for (int i = boneNodes.Count - 1; i > -1; i--)
                {
                    //Set Start Rotation
                    boneNodes[i].startRot = Quaternion.Inverse(boneNodes[i].nodeTransform.rotation) * rootNode.rotation;

                    //Leaf Node
                    if (i == boneNodes.Count - 1)
                    {
                        //Get Rotation to the target/next bone
                        boneNodes[i].startRotTarget = (Quaternion.Inverse(rootNode.rotation) * (target.position - rootNode.position)) - (Quaternion.Inverse(rootNode.rotation) * (boneNodes[i].nodeTransform.position - rootNode.position));
                    }
                    else
                    {
                        //Get Rotation to the target/next bone
                        boneNodes[i].startRotTarget = (Quaternion.Inverse(rootNode.rotation) * (boneNodes[i + 1].nodeTransform.position - rootNode.position)) - (Quaternion.Inverse(rootNode.rotation) * (boneNodes[i].nodeTransform.position - rootNode.position));
                        //Look at bone in front and get distance to it
                        boneNodes[i].initalDistanceToChild = boneNodes[i].startRotTarget.magnitude;
                        //Append distance to total
                        totalChainDistance += boneNodes[i].initalDistanceToChild;
                    }
                }

                //Get squared result for faster calc later on
                totalChainDistanceSquared = totalChainDistance * totalChainDistance;

            }
        }

        public static void IKStep(ref BoneStructure boneStructure)
        {
            Debug.Log($"I am the IKStep and I have {boneStructure.boneNodes.Count} bone nodes in my structure ||| ELEMENT [0] {boneStructure.boneNodes[0].node.name} LAST ELEMENT {boneStructure.boneNodes[boneStructure.boneNodes.Count - 1].node.name}");

            if (boneStructure.target == null)
            {
                Debug.LogError($"No Target Assigned For IKSystem on {boneStructure.boneNodes[boneStructure.boneNodes.Count - 1].node.name}");
                return;
            }

            //FABIK START

            //Store infomation in local vars for quicker access
            List<Vector3> nodePositions = new List<Vector3>();
            for (int i = 0; i < boneStructure.boneNodes.Count; i++)
            {
                nodePositions.Add(Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.boneNodes[i].nodeTransform.position - boneStructure.rootNode.position));
                Debug.Log($"Added {nodePositions[nodePositions.Count - 1]} to element {i} which has a name of {boneStructure.boneNodes[i].node.name}");
            }
            
            Vector3 targetPosition = (Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.target.position - boneStructure.rootNode.position));
            Quaternion targetRotation = (Quaternion.Inverse(boneStructure.target.rotation) * boneStructure.rootNode.rotation);

            //Is the target further than we can reach?
            if ((targetPosition - (Quaternion.Inverse(boneStructure.rootNode.rotation) * (nodePositions[0] - boneStructure.rootNode.position))).sqrMagnitude >= boneStructure.totalChainDistanceSquared)
            {
                //Go along a direction vector
                Vector3 dir = (boneStructure.target.position - nodePositions[0]).normalized;

                //Assign positions along vector, skip root
                for (int i = 1; i < nodePositions.Count; i++)
                {
                    Debug.Log($"Adjusting position of node {i}...");
                    nodePositions[i] = nodePositions[i - 1] + dir * boneStructure.boneNodes[i - 1].initalDistanceToChild;
                }
            }

            //Set Positions and Rotations
            for (int i = 0; i < nodePositions.Count; i++)
            {
                Debug.Log($"There is {boneStructure.boneNodes.Count} boneNodes and {nodePositions.Count} node Positions");
                Debug.Log($"adjusting node [{i}] to {boneStructure.rootNode.rotation * nodePositions[i] + boneStructure.rootNode.position} found by {boneStructure.rootNode.rotation} * {nodePositions[i]} + {boneStructure.rootNode.position}");
                
                //Rotations

                //Positions
                boneStructure.boneNodes[i].nodeTransform.position = boneStructure.rootNode.rotation * nodePositions[i] + boneStructure.rootNode.position;
            }

            //FABIK END

            Debug.Log("Done.");

        }

        //private Vector3 GetPositionRootSpace(Transform current)
        //{
        //    if (Root == null)
        //        return current.position;
        //    else
        //        return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
        //}

        //private void SetPositionRootSpace(Transform current, Vector3 position)
        //{
        //    if (Root == null)
        //        current.position = position;
        //    else
        //        current.position = Root.rotation * position + Root.position;
        //}

        //private Quaternion GetRotationRootSpace(Transform current)
        //{
        //    //inverse(after) * before => rot: before -> after
        //    if (Root == null)
        //        return current.rotation;
        //    else
        //        return Quaternion.Inverse(current.rotation) * Root.rotation;
        //}

        //private void SetRotationRootSpace(Transform current, Quaternion rotation)
        //{
        //    if (Root == null)
        //        current.rotation = rotation;
        //    else
        //        current.rotation = Root.rotation * rotation;
        //}

        //Move Bones Towards target
        //public static void IKStep(ref List<BoneNode> boneNodes, ref Transform target, ref Transform bendTarget, ref bool shouldBend, ref int solverIterations, ref float solvedDistanceThreshold)
        //{
        //    UnityEngine.Debug.Log($"Hello I am reporting that I have {boneNodes.Count} nodes");

        //    if (target == null)
        //    {
        //        UnityEngine.Debug.LogError($"[EzyIK] Cannot perform IKStep on object {boneNodes[0].node.name} as it does not have a target");
        //        return;
        //    }

        //    //===============
        //    // BEGIN CALC
        //    //===============
        //    //Based on the FABRIK algorithm

        //    //FIND ROTATION DIFF

        //    Quaternion rootRot = Quaternion.identity;
        //    if (boneNodes[boneNodes.Count - 1].nodeTransform.parent != null)
        //    {
        //        rootRot = boneNodes[boneNodes.Count - 1].nodeTransform.parent.rotation;
        //    }
        //    Quaternion rootRotDiff = rootRot * Quaternion.Inverse(rootRot);

        //    //MOVEMENT

        //    //If it is further than we can reach (using squared is faster)
        //    if ((target.position - boneNodes[boneNodes.Count - 1].nodeTransform.position).sqrMagnitude >= boneNodes[0].totalChainDistanceSquared)
        //    {
        //        //Get direction from root node to target
        //        Vector3 dir = (target.position - boneNodes[boneNodes.Count - 1].nodeTransform.position).normalized;

        //        //Set positions along a line according to the target's direction from our root node (do not move the root node)
        //        for (int i = boneNodes.Count - 2; i > -1; i--)
        //        {
        //            boneNodes[i].nodeTransform.position = boneNodes[i + 1].nodeTransform.position + dir * boneNodes[i].initalDistanceFromParent;
        //        }

        //    }

        //    //If it is within our reach
        //    else
        //    {
        //        float solvedSqrThreshold = solvedDistanceThreshold * solvedDistanceThreshold;

        //        //For each solver iteration
        //        for (int i = 0; i < solverIterations; i++)
        //        {
        //            //Are close enough to our target?
        //            if ((boneNodes[0].nodeTransform.position - target.position).sqrMagnitude < solvedSqrThreshold)
        //            {
        //                break;
        //            }

        //            //Backwards check (starts from leaf and goes to root) we can ignore moving the root bone so that it stays unaffected
        //            for (int j = 0; j < boneNodes.Count - 1; j++)
        //            {
        //                //Debug.Log($"Back {j}");
        //                //Set leaf node ontop of target
        //                if (j == 0)
        //                {
        //                    boneNodes[0].nodeTransform.position = target.position;
        //                }
        //                //Look at previous node and move towards it according to our distance
        //                else
        //                {
        //                    boneNodes[j].nodeTransform.position = boneNodes[j - 1].nodeTransform.position + (boneNodes[j].nodeTransform.position - boneNodes[j - 1].nodeTransform.position).normalized * boneNodes[j - 1].initalDistanceFromParent;
        //                }
        //            }

        //            //Forwards check (starts from root and goes to leaf)
        //            for (int j = boneNodes.Count - 2; j > -1; j--)
        //            {
        //                //Look at node in front of us and move according to our distance
        //                boneNodes[j].nodeTransform.position = boneNodes[j + 1].nodeTransform.position + (boneNodes[j].nodeTransform.position - boneNodes[j + 1].nodeTransform.position).normalized * boneNodes[j].initalDistanceFromParent;
        //            }

        //        }
        //    }

        //    //UPDATE ROTATIONS
        //    for (int i = boneNodes.Count - 1; i > -1; i--)
        //    {


        //        //If we are effecting the leaf node
        //        if (i == 0)
        //        {
        //            Debug.Log("Rotating the leaf...");
        //            //boneNodes[i].nodeTransform.rotation = boneNodes[boneNodes.Count - 1].nodeTransform.rotation * (Quaternion.Inverse(target.rotation) * boneNodes[i].startRotTarget * Quaternion.Inverse(boneNodes[i].startRot));
        //        }
        //        //If we are effecting the other nodes
        //        else
        //        {
        //            //boneNodes[i].nodeTransform.rotation = boneNodes[boneNodes.Count - 1].nodeTransform.rotation * (Quaternion.FromToRotation(boneNodes[i].startDir, boneNodes[i - 1].nodeTransform.position - boneNodes[i].nodeTransform.position) * Quaternion.Inverse(boneNodes[i].startRot));
        //        }
        //    }

        //    //===============
        //    // END CALC
        //    //===============


        //}

    }
}
