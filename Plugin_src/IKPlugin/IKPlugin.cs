using System;
using System.Collections.Generic;
using UnityEngine;

namespace HenryIK
{
    public class IKPlugin
    {

        public struct BoneNode
        {
            public GameObject node;
            public Transform nodeTransform;
            public Transform parentTransform;
            public float initalDistanceFromParent;
            public bool isRootNode;

            public BoneNode(ref GameObject _node)
            {
                node = _node;
                nodeTransform = node.transform;
                isRootNode = false;
                //If we have a parent
                if (node.transform.parent)
                {
                    parentTransform = node.transform.parent.transform;
                    initalDistanceFromParent = Vector3.Distance(nodeTransform.position, parentTransform.position);
                }
                else
                {
                    parentTransform = null;
                    initalDistanceFromParent = 0.0f;
                    isRootNode = true;
                }
            }
        }

        //Move Bones Towards target
        public void IKStep(ref List<BoneNode> boneNodes)
        {
            UnityEngine.Debug.Log($"Hello I am reporting that I have {boneNodes.Count} nodes");
        }

        public static void Test()
        {
            UnityEngine.Debug.Log("Hello Debug!");
        }
    }
}
