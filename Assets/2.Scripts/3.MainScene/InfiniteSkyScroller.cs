// InfiniteSkyScroller.cs
// Quad + Unlit 머티리얼 조합. 이 셰이더는 mainTextureOffset을 정상적으로 반영함.
using UnityEngine;

public class InfiniteSkyScroller : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.02f; // 초당 오프셋 이동량

    public MeshRenderer mesh;

    private Material material;

    private void Awake()
    {
        if (mesh == null) mesh = GetComponent<MeshRenderer>();
        material = mesh.material; // 인스턴스를 한 번만 생성해 캐싱
    }

    private void Update()
    {
        material.mainTextureOffset += new Vector2(scrollSpeed * Time.deltaTime, 0);
    }
}