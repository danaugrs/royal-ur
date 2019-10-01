using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Button : MonoBehaviour {
    
    public UnityEvent invokeMethod; // Set in editor

    void OnMouseDown() 
    {
        invokeMethod.Invoke();
    }
}
