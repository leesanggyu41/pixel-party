using UnityEngine;
using Fusion;

public struct PlayerNetworkInput : INetworkInput
{
    public float MoveX;
    public NetworkBool JumpInput;
}
