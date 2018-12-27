using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Position : MonoBehaviour {

    public Position nextPlayer1Spot; // The next board position for Player1
    public Position nextPlayer2Spot; // The next board position for Player2
    public Position nextEndSpot;     // The next end position to be used
    public Position nextStartSpot;   // The next start position to be used
    public Piece piece;              // The piece occupying this position if any
    public bool mandala = false;     // Whether this position has a mandala
    public bool endSpot;             // Whether this position is an end spot

    // Utility function to set the next board position for both players at once
    public void SetNext(Position next) {
        nextPlayer1Spot = next;
        nextPlayer2Spot = next;
    }

}
