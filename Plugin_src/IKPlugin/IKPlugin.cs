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
            public float initalDistanceFromParent;
            public bool isRootNode;
            public float totalChainDistance;
            public float totalChainDistanceSquared;

            //If we have left the bone section do not go any further upwards
            public BoneNode(ref GameObject _node, ref int maxDepth, ref int currentDepth)
            {
                node = _node;
                nodeTransform = node.transform;
                isRootNode = false;
                totalChainDistance = 0.0f;
                totalChainDistanceSquared = 0.0f;

                //If we have a parent and have not gone past our max depth
                if (node.transform.parent && (currentDepth < maxDepth || maxDepth < 0))
                {
                    parentTransform = node.transform.parent.transform;
                    initalDistanceFromParent = Vector3.Distance(nodeTransform.position, parentTransform.position);
                    currentDepth++; 
                }
                else
                {
                    parentTransform = null;
                    initalDistanceFromParent = 0.0f;
                    isRootNode = true;
                }
            }
        }

        //Initaliser
        public static List<BoneNode> Init(ref GameObject startBone, ref int maxDepth)
        {
            //Init vars
            List<BoneNode> bones = new List<BoneNode>();
            int currentDepth = 0;

            //Add first bone
            bones.Add(new BoneNode(ref startBone, ref maxDepth, ref currentDepth));
            
            //Find all bones
            //While we haven't found the rootnode
            while (!bones[bones.Count - 1].isRootNode)
            {
                //Go up the chain
                var tmp = bones[bones.Count - 1].node.transform.parent.gameObject;
                //Add new bone
                if (maxDepth < 0)
                {
                    bones.Add(new BoneNode(ref tmp, ref maxDepth, ref currentDepth));
                }
                //We have a limit to how far we go
                else if (currentDepth <= maxDepth)
                {
                    bones.Add(new BoneNode(ref tmp, ref maxDepth, ref currentDepth));
                }
                //Exit while loop
                else
                {
                    break;
                }
            }

            //Get the total chain length and put infomation into our leaf node
            float distance = 0.0f;

            for (int i = 0; i < bones.Count; i++)
            {
                distance += bones[i].initalDistanceFromParent;
            }

            bones[0].totalChainDistance = distance;
            bones[0].totalChainDistanceSquared = distance * distance;

            //Return list
            return bones;
        }

        //Move Bones Towards target
        public static void IKStep(ref List<BoneNode> boneNodes, ref Transform target, ref Transform bendTarget, ref bool shouldBend, ref int solverIterations, ref float solvedDistanceThreshold)
        {
            UnityEngine.Debug.Log($"Hello I am reporting that I have {boneNodes.Count} nodes");

            if (target == null)
            {
                UnityEngine.Debug.LogError($"[EzyIK] Cannot perform IKStep on object {boneNodes[0].node.name} as it does not have a target");
                return;
            }

            //===============
            // BEGIN CALC
            //===============
            //Based on the FABRIK algorithm

            //If it is further than we can reach (using squared is faster)
            if ((target.position - boneNodes[boneNodes.Count - 1].nodeTransform.position).sqrMagnitude >= boneNodes[0].totalChainDistanceSquared)
            {
                //Get direction from root node to target
                Vector3 dir = (target.position - boneNodes[boneNodes.Count - 1].nodeTransform.position).normalized;

                //Set positions along a line according to the target's direction from our root node (do not move the root node)
                for (int i = boneNodes.Count - 2; i > -1; i--)
                {
                    boneNodes[i].nodeTransform.position = boneNodes[i + 1].nodeTransform.position + dir * boneNodes[i].initalDistanceFromParent;
                }

            }

            //If it is within our reach
            else
            {
                float solvedSqrThreshold = solvedDistanceThreshold * solvedDistanceThreshold;

                //For each solver iteration
                for (int i = 0; i < solverIterations; i++)
                {
                    //Are close enough to our target?
                    if ((boneNodes[0].nodeTransform.position - target.position).sqrMagnitude < solvedSqrThreshold)
                    {
                        break;
                    }

                    //Backwards check (starts from leaf and goes to root) we can ignore moving the root bone so that it stays unaffected
                    for (int j = 0; j < boneNodes.Count - 1; j++)
                    {
                        //Debug.Log($"Back {j}");
                        //Set leaf node ontop of target
                        if (j == 0)
                        {
                            boneNodes[0].nodeTransform.position = target.position;
                        }
                        //Look at previous node and move towards it according to our distance
                        else
                        {
                            boneNodes[j].nodeTransform.position = boneNodes[j - 1].nodeTransform.position + (boneNodes[j].nodeTransform.position - boneNodes[j - 1].nodeTransform.position).normalized * boneNodes[j - 1].initalDistanceFromParent;
                        }
                    }

                    //Forwards check (starts from root and goes to leaf)
                    for (int j = boneNodes.Count - 2; j > -1; j--)
                    {
                        Debug.Log($"Front {j}");
                        //Look at node in front of us and move according to our distance
                        boneNodes[j].nodeTransform.position = boneNodes[j + 1].nodeTransform.position + (boneNodes[j].nodeTransform.position - boneNodes[j + 1].nodeTransform.position).normalized * boneNodes[j].initalDistanceFromParent;
                    }

                }
            }

            //===============
            // END CALC
            //===============


        }

        public static void Test()
        {
            UnityEngine.Debug.Log("Hello Debug I Am IKStep!");
        }
    }
}
