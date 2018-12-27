using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dice : MonoBehaviour {

    // The four dice
    public Die die1;
    public Die die2;
    public Die die3;
    public Die die4;

    public Game game;
    public AudioSource diceSound;

    public float rollDuration = 0.14f;
    public int rollValue;

    // Initialize dice randomly
    void Start () {

        Reset();
    }

    // Reset the four dice randomly
    public void Reset() {

        die1.rollDuration = 0;
        die2.rollDuration = 0;
        die3.rollDuration = 0;
        die4.rollDuration = 0;
        die1.rollEnded = true;
        die2.rollEnded = true;
        die3.rollEnded = true;
        die4.rollEnded = true;
        die1.transform.rotation = Random.rotationUniform;
        die2.transform.rotation = Random.rotationUniform;
        die3.transform.rotation = Random.rotationUniform;
        die4.transform.rotation = Random.rotationUniform;
        die1.SetActive(Random.value > 0.5);
        die2.SetActive(Random.value > 0.5);
        die3.SetActive(Random.value > 0.5);
        die4.SetActive(Random.value > 0.5);
        die1.NormalizeRoll();
        die2.NormalizeRoll();
        die3.NormalizeRoll();
        die4.NormalizeRoll();
    }

    public void Roll() {

        diceSound.Play();
        game.state = GameState.Rolling;

        int result = die1.Roll(rollDuration);
        result += die2.Roll(2 * rollDuration);
        result += die3.Roll(3 * rollDuration);
        result += die4.Roll(4 * rollDuration);
        rollValue = result;
        Debug.Log("Rolling a " + rollValue);
    }

    // OnMouseDown is called when the dice are clicked or tapped on
    void OnMouseDown() {

        if (game.state == GameState.AwaitingRoll && !game.Paused()) {
            Roll();
        }
    }

    public void SetGlowColor(Color color) {

        die1.pointLight.color = color;
        die2.pointLight.color = color;
        die3.pointLight.color = color;
        die4.pointLight.color = color;
    }

}
