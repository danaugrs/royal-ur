using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Die : MonoBehaviour {

    public float rollDuration;
    public bool rollEnded = true;

    public Material NormalMaterial;
    public Material TrueMaterial;

    public Game game;
    public Light pointLight;
    public AnimationCurve glowCurve;
    private float glowTimer = 0.0f;
    public bool lastDie;

    // Update is called once per frame
    void Update() {

        if (!game.Paused()) {

            // Check if need to rotate the die
            if (rollDuration > 0) {
                rollEnded = false;
                rollDuration -= Time.deltaTime;
                transform.rotation = Random.rotationUniform;
            } else if (!rollEnded) {
                rollDuration = 0;
                rollEnded = true;
                RollEnd();
            }

            // Glow Effect
            glowTimer += Time.deltaTime;
            if (game.state == GameState.AwaitingRoll) {// && ( game.type == GameType.Multiplayer || game.player == Player.One ) ) {
                pointLight.intensity = 10 * glowCurve.Evaluate(2 * glowTimer);
            } else {
                pointLight.intensity = 0;
                glowTimer = 0;
            }
        }
    }

    // NormalizeRoll resets the rotation of the die in all axes except the vertical
    // This makes it so that the die has a flat face on the ground (the same flat face every time)
    public void NormalizeRoll() {

        Quaternion swing = Quaternion.identity;
        Quaternion twist = Quaternion.identity;
        Util.SwingTwistDecomposition(transform.rotation, Vector3.up, out swing, out twist);
        transform.rotation = twist;
    }

    // RollEnd is called when the die stopped rolling
    // If it's the last die, then it invokes game.Roll()
    public void RollEnd() {

        NormalizeRoll();

        if (lastDie) {
            game.Roll();
        }
    }

    // Roll randomly selects a final value (0/1) and starts rolling the die
    public int Roll(float duration) {

        int state = 0;
        if (Random.value > 0.5) {
            state = 1;
        }
        SetActive(state != 0);
        rollDuration = duration;
        return state;
    }

    // SetActive switches the material of the two active dots (one of which always faces up)
    // according to the specified state
    public void SetActive(bool state) {
        
        Renderer dieTrueRend = transform.Find("DieTrue").gameObject.GetComponent<Renderer>();
        Renderer dieFalseRend = transform.Find("DieFalse").gameObject.GetComponent<Renderer>();
        if (state) {
            dieTrueRend.material = TrueMaterial;
            dieFalseRend.material = NormalMaterial;
        } else {
            dieTrueRend.material = NormalMaterial;
            dieFalseRend.material = TrueMaterial;
        }
    }

}
