using System;
using UnityEngine;

namespace HenryIK
{
    public class IKPlugin
    {

        struct BoneNode
        {

            public GameObject node;
            public Transform nodeTransform;
            public Transform parentTransform;
            public float initalDistanceFromParent;
            public bool isRootNode;

            public BoneNode(GameObject _node)
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

        public static void Test()
        {
            UnityEngine.Debug.Log("Hello Debug!");
        }
    }
}
