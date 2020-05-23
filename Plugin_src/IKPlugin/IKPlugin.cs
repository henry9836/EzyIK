using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace HenryIK
{
    public class IKPlugin
    {
        public class BoneNode
        {
            public GameObject node;
            public Transform nodeTransform;
            public Quaternion startRot;
            public Vector3 startDirTarget;
            public float initalDistanceToChild = 0.0f;

            //If we have left the bone section do not go any further upwards
            public BoneNode(ref GameObject _node)
            {
                node = _node;
                nodeTransform = node.transform;
            }
        }

        public class BoneStructure
        {
            public enum MoveType
            {
                LINEAR,
                CUSTOM
            }


            public List<BoneNode> boneNodes = new List<BoneNode>();
            public MoveType moveType;
            public AnimationCurve moveCurve;
            public AnimationCurve rotCurve;
            public Quaternion startTargetRot;
            public Transform rootNode;
            public Transform target;
            public Transform bendTarget;
            public float totalChainDistance;
            public float totalChainDistanceSquared;
            public float arriveThreshold;
            public float arriveThresholdSquared;
            public float nodeMoveSpeed;
            public float rotMoveSpeed;
            public int maxDepth = -1;
            public int maxSolveIterations = 5;


            //Create a bonestructure
            public BoneStructure(ref GameObject startBone, ref int maxDepth, ref Transform _target, ref Transform _bendTarget, ref float _arriveThreshold, ref int _maxSolveIterations, ref float _moveSpeed, ref float _rotSpeed, ref MoveType _moveType, ref AnimationCurve _moveCurve, ref AnimationCurve _rotCurve)
            {
                //Init here
                if (_target == null)
                {
                    Debug.LogError("Cannot create bone structure as there is no target");
                }

                int currentDepth = 0;
                target = _target;
                Transform tmpRoot = startBone.transform;
                arriveThreshold = _arriveThreshold;
                arriveThresholdSquared = arriveThreshold * arriveThreshold;
                maxSolveIterations = _maxSolveIterations;
                nodeMoveSpeed = _moveSpeed;
                moveType = _moveType;
                moveCurve = _moveCurve;
                rotCurve = _rotCurve;
                if (_rotSpeed > 0.0f) {
                    rotMoveSpeed = _rotSpeed;
                }
                else
                {
                    rotMoveSpeed = nodeMoveSpeed;
                }

                if (_bendTarget != null)
                {
                    bendTarget = _bendTarget;
                }

                //While we haven't found the root node
                while (rootNode == null)
                {
                    //Append to our list of bone nodes
                    var tmpNode = tmpRoot.gameObject; 
                    boneNodes.Add(new BoneNode(ref tmpNode));

                    //Go up the chain until we find the root node or reach our max depth search
                    if (tmpRoot.parent == null || (currentDepth > maxDepth && maxDepth != -1))
                    {
                        rootNode = tmpRoot;
                        break;
                    }
                    tmpRoot = tmpRoot.parent;
                    currentDepth++;
                }

                //Invert our list of bones for easier FABIK logic
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
                        boneNodes[i].startDirTarget = (Quaternion.Inverse(rootNode.rotation) * (target.position - rootNode.position)) - (Quaternion.Inverse(rootNode.rotation) * (boneNodes[i].nodeTransform.position - rootNode.position));
                    }  
                    else
                    {
                        //Get Rotation to the target/next bone
                        boneNodes[i].startDirTarget = (Quaternion.Inverse(rootNode.rotation) * (boneNodes[i + 1].nodeTransform.position - rootNode.position)) - (Quaternion.Inverse(rootNode.rotation) * (boneNodes[i].nodeTransform.position - rootNode.position));
                        //Look at bone in front and get distance to it
                        boneNodes[i].initalDistanceToChild = boneNodes[i].startDirTarget.magnitude;
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
            }

            Vector3 targetPosition = Vector3.zero;
            Quaternion targetRotation = Quaternion.identity;

            //No movespeed specified
            if (boneStructure.nodeMoveSpeed < 0.0f){

                targetPosition = (Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.target.position - boneStructure.rootNode.position));
                targetRotation = (Quaternion.Inverse(boneStructure.target.rotation) * boneStructure.rootNode.rotation);
            }
            //Move slower towards target
            else if (boneStructure.moveType == BoneStructure.MoveType.LINEAR)
            {
                targetPosition = (Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.target.position - boneStructure.rootNode.position));
                targetPosition = Vector3.MoveTowards(nodePositions[nodePositions.Count - 1], targetPosition, boneStructure.nodeMoveSpeed * Time.deltaTime);
                targetRotation = (Quaternion.Inverse(boneStructure.target.rotation) * boneStructure.rootNode.rotation);
                targetRotation = Quaternion.RotateTowards(boneStructure.boneNodes[boneStructure.boneNodes.Count - 1].nodeTransform.rotation, targetRotation, boneStructure.rotMoveSpeed * Time.deltaTime);
            }
            //Move along a graph
            else if (boneStructure.moveType == BoneStructure.MoveType.CUSTOM)
            {
                targetPosition = (Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.target.position - boneStructure.rootNode.position));
                targetPosition = Vector3.MoveTowards(nodePositions[nodePositions.Count - 1], targetPosition, boneStructure.nodeMoveSpeed * boneStructure.moveCurve.Evaluate(Vector3.Distance(nodePositions[nodePositions.Count - 1], targetPosition)) * Time.deltaTime);
                targetRotation = (Quaternion.Inverse(boneStructure.target.rotation) * boneStructure.rootNode.rotation);
                targetRotation = Quaternion.RotateTowards(boneStructure.boneNodes[boneStructure.boneNodes.Count - 1].nodeTransform.rotation, targetRotation, boneStructure.rotMoveSpeed * boneStructure.rotCurve.Evaluate(Vector3.Distance(nodePositions[nodePositions.Count - 1], targetPosition)) * Time.deltaTime);
            }
            //Fail safe
            else
            {
                Debug.LogError($"No behaviour set up for moveType {boneStructure.moveType} defaulting to no moveType behaviour");
                targetPosition = (Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.target.position - boneStructure.rootNode.position));
                targetRotation = (Quaternion.Inverse(boneStructure.target.rotation) * boneStructure.rootNode.rotation);
            }

            //Is the target further than we can reach?
            if ((targetPosition - (Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.boneNodes[0].nodeTransform.position - boneStructure.rootNode.position))).sqrMagnitude >= boneStructure.totalChainDistanceSquared)
            {
                //Go along a direction vector
                Vector3 dir = (targetPosition - nodePositions[0]).normalized;

                //Assign positions along vector, skip root
                for (int i = 1; i < nodePositions.Count; i++)
                {
                    nodePositions[i] = nodePositions[i - 1] + dir * boneStructure.boneNodes[i - 1].initalDistanceToChild;
                }
            }
            //If the target is within our range
            else
            {
                //For the amount of iterations
                for (int i = 0; i < boneStructure.maxSolveIterations; i++)
                {
                    //Backwards check (starts from leaf and goes to root) we can ignore moving the root bone so that it stays unaffected 
                    for (int j = nodePositions.Count - 1; j > 0; j--)
                    {
                        //if leaf node
                        if (j == nodePositions.Count - 1)
                        {
                            //Set on top of target
                            nodePositions[j] = targetPosition;
                        }
                        //Look at child node and move towards it according to our distance 
                        else
                        {
                            //Move node onto a direction line from it's child node with the distance being the same as it's inital distance from the child
                            nodePositions[j] = nodePositions[j + 1] + (nodePositions[j] - nodePositions[j + 1]).normalized * boneStructure.boneNodes[j].initalDistanceToChild;
                        }
                    }

                    //Forwards check (starts from root and goes to leaf) 
                    for (int j = 1; j < nodePositions.Count - 1; j++)
                    {
                        //Look at parent node and move according to our distance 
                        nodePositions[j] = nodePositions[j - 1] + (nodePositions[j] - nodePositions[j - 1]).normalized * boneStructure.boneNodes[j - 1].initalDistanceToChild;
                    }

                    //Is our leaf node closer than our arrive threshold to the target?
                    if ((nodePositions[nodePositions.Count - 1] - targetPosition).sqrMagnitude < boneStructure.arriveThresholdSquared)
                    {
                        break;
                    }
                }
            }

            //Bend Target
            //To bend towards a bend target we can project points on a plane and move towards the bend target
            if (boneStructure.bendTarget != null)
            {
                //For all nodes, except root
                for (int i = 1; i < nodePositions.Count - 1; i++)
                {
                    //Create the projection plane
                    Plane projectionPlane = new Plane(nodePositions[i + 1] - nodePositions[i - 1], nodePositions[i - 1]);
                    //Get Positions
                    Vector3 projectedBendLine = projectionPlane.ClosestPointOnPlane(Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.bendTarget.position - boneStructure.rootNode.position));
                    Vector3 projectedNode = projectionPlane.ClosestPointOnPlane(Quaternion.Inverse(boneStructure.rootNode.rotation) * (boneStructure.boneNodes[i].nodeTransform.position - boneStructure.rootNode.position));
                    //Find the angle with the shortest distance to the bend target
                    float angle = Vector3.SignedAngle(projectedNode - nodePositions[i - 1], projectedBendLine - nodePositions[i - 1], projectionPlane.normal);
                    //Rotate the parent node so that our node is the closest to the bend target that is can be
                    nodePositions[i] = Quaternion.AngleAxis(angle, projectionPlane.normal) * (nodePositions[i] - nodePositions[i - 1]) + nodePositions[i - 1];
                }
            }

            //Set Positions and Rotations
            for (int i = 0; i < nodePositions.Count; i++)
            {

                //Rotations
                //Leaf
                if (i == nodePositions.Count - 1)
                {
                    boneStructure.boneNodes[i].nodeTransform.rotation = boneStructure.rootNode.rotation * (Quaternion.Inverse(targetRotation) * boneStructure.startTargetRot * Quaternion.Inverse(boneStructure.boneNodes[i].startRot));
                }
                else
                {
                    boneStructure.boneNodes[i].nodeTransform.rotation = boneStructure.rootNode.rotation * (Quaternion.FromToRotation(boneStructure.boneNodes[i].startDirTarget, nodePositions[i + 1] - nodePositions[i]) * Quaternion.Inverse(boneStructure.boneNodes[i].startRot));
                }

                //Positions
                boneStructure.boneNodes[i].nodeTransform.position = boneStructure.rootNode.rotation * nodePositions[i] + boneStructure.rootNode.position;
            }

            //FABIK END
        }
    }
}
