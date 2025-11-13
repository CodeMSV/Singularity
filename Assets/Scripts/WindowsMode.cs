using UnityEngine;

public class ForceWindow : MonoBehaviour
{
    void Awake()
    {

        Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
    }
}