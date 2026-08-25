using UnityEngine;
using Fusion;

public class NetworkRunnerFactory : MonoBehaviour
{
    public static NetworkRunner Create(string runnerName)
    {
        GameObject runnerObject = new GameObject(runnerName);
        Object.DontDestroyOnLoad(runnerObject);
        NetworkRunner runner = runnerObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true; // Set ProvideInput to true
        return runner;
    }
    public static void Destroy(NetworkRunner runner)
    {
        if (runner != null)
        {
            Destroy(runner.gameObject);
        }
        
    }

}
