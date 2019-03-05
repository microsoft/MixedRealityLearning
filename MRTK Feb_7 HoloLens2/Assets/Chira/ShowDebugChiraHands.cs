using System;
using UnityEngine;
namespace Chira
{
    public class ShowDebugChiraHands : Singleton<ShowDebugChiraHands>
    {
        private GameObject[] joints;
        private GameObject[] handMeshes;
        private GameObject handMeshPrefab;
        private GameObject cubePrefab;

        public enum ShowHandsMode
        {
            Joints,
            HandMesh,
            Hidden,
            ShowHandsModeCount
        };
        private ShowHandsMode HandMode = ShowHandsMode.Joints;

        private void NextHandMode()
        {
            SetHandMode((ShowHandsMode)(((int)HandMode + 1) % (int)ShowHandsMode.ShowHandsModeCount));
        }

        public void SetHandMode(ShowHandsMode mode)
        {
            HandMode = (ShowHandsMode)(((int)HandMode + 1) % (int)ShowHandsMode.ShowHandsModeCount);
            Debug.Log("HandMode is now " + HandMode);
            OnHandModeChanged();
        }

        private void OnHandModeChanged()
        {
            if (joints != null)
            {
                foreach (var j in joints)
                {
                    j.SetActive(HandMode == ShowHandsMode.Joints);
                }

            }
            if (handMeshes != null)
            {
                foreach (var m in handMeshes)
                {
                    m.SetActive(HandMode == ShowHandsMode.HandMesh);
                }
            }
        }

        protected void Awake()
        {
            cubePrefab = Resources.Load("ChiraDebugJoint") as GameObject;
            handMeshPrefab = Resources.Load("ChiraMesh") as GameObject;
        }



        private void Start()
        {
            InitializeJoints();
            InitializeHandMesh();
            ChiraDataProvider.Instance.OnChiraDataChanged += OnChiraDataChanged;
        }

        private void OnChiraDataChanged()
        {
            ChiraDataUnity chiraData = ChiraDataProvider.Instance.CurrentFrame;
            if (chiraData == null)
            {
                return;
            }

            if (HandMode == ShowHandsMode.Joints)
            {
                for (var i = 0; i < joints.Length; i++)
                {
                    joints[i].transform.position = chiraData.Joints[i];
                    bool isTracked = ChiraDataUtils.IsHandTracked(IsJointRight(i) ? HandSide.Right : HandSide.Left);
                    joints[i].GetComponent<MeshRenderer>().enabled = isTracked;
                }
            }

            if (HandMode == ShowHandsMode.HandMesh)
            {
                const int vertCount = ChiraDataUnity.MaxVertices / ChiraDataUnity.MaxHands;
                for (var i = 0; i < handMeshes.Length; i++)
                {
                    GameObject handMeshObj = handMeshes[i];
                    bool isTracked = ChiraDataUtils.IsHandTracked(i == 1 ? HandSide.Right : HandSide.Left);
                    if (isTracked)
                    {
                        Mesh mesh = handMeshObj.GetComponent<MeshFilter>().mesh;
                        mesh.Clear();

                        Vector3[] vertices = new Vector3[vertCount];
                        for (var j = 0; j < vertCount; j++)
                        {
                            vertices[j] = chiraData.Vertices[j + i * vertCount];
                        }
#if !UNITY_EDITOR && UNITY_WSA
                    mesh.vertices = vertices;
                    mesh.triangles = HandTracking.ChiraAPI.Vertices;
                    mesh.RecalculateNormals();
#endif
                        handMeshObj.SetActive(true);
                    }
                    else
                    {
                        handMeshObj.SetActive(false);
                    }
                }
            }
        }

        private bool IsJointRight(int i)
        {
            return i >= (int)Joints.Count / 2;
        }

        public void ChangeHandMode()
        {
            NextHandMode();
        }

        private void InitializeHandMesh()
        {
            handMeshes = new GameObject[ChiraDataUnity.MaxHands];
            for (int i = 0; i < handMeshes.Length; i++)
            {
                handMeshes[i] = Instantiate(handMeshPrefab);
                handMeshes[i].transform.parent = gameObject.transform;
                handMeshes[i].GetComponent<MeshFilter>().mesh.Clear();
            }
        }

        private void InitializeJoints()
        {
            joints = new GameObject[(int)Joints.Count];
            for (int i = 0; i < joints.Length; i++)
            {
                joints[i] = Instantiate(cubePrefab);
                joints[i].transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                joints[i].transform.parent = gameObject.transform;
            }
        }
    } 
}