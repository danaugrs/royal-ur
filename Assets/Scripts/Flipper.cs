using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flipper : MonoBehaviour {

    public Game game;

    public bool targetPos = true; // True is Up, False is Down
    private bool settled = true;

    private float timer;

    public AnimationCurve animCurve;
    public Vector3 posUp;
    public Vector3 posDown;
    public float duration;

    public AudioSource flipSound;


    // Use this for initialization
    void Start () {
        timer = duration;
    }

    // Update is called once per frame
    void Update () {

        if (!settled) {
            float val = animCurve.Evaluate(timer / duration); ;
            if (targetPos) {
                transform.rotation = Quaternion.Euler(Vector3.Slerp(posDown, posUp, val));
            } else {
                transform.rotation = Quaternion.Euler(Vector3.Slerp(posUp, posDown, val));
            }
            if (val == 1.0f) {
                settled = true;
                timer = duration;
            }
            timer += Time.deltaTime;
        }
    }

    public void Flip(bool buttonVertPos) {

        if (buttonVertPos != targetPos && game.state == GameState.Menu) {
            flipSound.Play();
            targetPos = !targetPos;
            settled = false;
            timer = duration - timer;
        }
    }    

}
