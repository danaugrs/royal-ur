using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {

    public Position position;
    public Game game;
    public Player player;

    public Material PlayableMaterial;
    public Material NormalMaterial;

    private bool playable = false;

    Renderer rend;

    // Use this for initialization
    void Start () {

        rend = gameObject.GetComponent<Renderer>();
    }

    // SetPlayable sets whether this piece is currently playable or not
    public void SetPlayable(bool state) {

        playable = state;            
        if (playable) {
            // Change material to highlight piece
            rend.material = PlayableMaterial;
        } else {
            rend.material = NormalMaterial;
        }
    }

    // CanMove checks that this piece can be moved the specified amount of tiles
    public bool CanMove(int rollValue) {

        Position finalPos = FinalPos(rollValue);
        return PosAvailable(finalPos);
    }

    // PosAvailable returns whether the specified position is available for this piece to go to
    public bool PosAvailable(Position pos) {

        return (pos != null) && ( (pos.piece == null) || ((pos.piece.player != player) && (!pos.mandala)) );
    }

    // FinalPos returns the final position if this piece were to move rollValue positions forward
    public Position FinalPos(int rollValue) {

        Position finalPos = position;
        while (rollValue > 0) {
            if (player == Player.One) {
                if (finalPos.nextPlayer1Spot == null) {
                    return null;
                }
                finalPos = finalPos.nextPlayer1Spot;
            } else { // Player.Two
                if (finalPos.nextPlayer2Spot == null) {
                    return null;
                }
                finalPos = finalPos.nextPlayer2Spot;
            }
            rollValue -= 1;
        }
        return finalPos;
    }

    // DistanceToEnd returns how many positions there are until the end spot
    public int DistanceToEnd() {

        int dist = 0;
        Position finalPos = position;
        while (true) {
            if (player == Player.One) {
                if (finalPos.nextPlayer1Spot == null) {
                    return dist;
                }
                finalPos = finalPos.nextPlayer1Spot;
            } else { // Player.Two
                if (finalPos.nextPlayer2Spot == null) {
                    return dist;
                }
                finalPos = finalPos.nextPlayer2Spot;
            }
            dist += 1;
        }
    }

    // Move setups the animation to move this piece the specified amount of tiles.
    // If an enemy piece is in the ending tile, the enemy piece is returned to the enemy's start.
    public void Move(int rollValue) {

        Debug.Log("Moving " + rollValue);
        Position finalPos = position;
        while (rollValue > 0) {
            if (player == Player.One) {
                finalPos = finalPos.nextPlayer1Spot;
            } else { // Player.Two
                finalPos = finalPos.nextPlayer2Spot;
            }
            rollValue -= 1;
        }
        
        if (finalPos.piece != null && finalPos.piece.player != player) { // If enemy piece on spot - capture it and send it to enemy start

            game.captureSound.Play();

            Position leftmostStart = game.LeftmostEmptyStartSpot(finalPos.piece.player);
            Piece enemyPiece = finalPos.piece;
            enemyPiece.position = leftmostStart;
            enemyPiece.transform.position = leftmostStart.transform.position;
            leftmostStart.piece = enemyPiece;
        } else {

            game.moveSound.Play();
        }
        
        position.piece = null;
        finalPos.piece = this;
        position = finalPos;
        transform.position = position.transform.position;

        if (finalPos.endSpot) { // If endSpot
            game.UpdateEndSpot(finalPos);
        }

        Debug.Log("FinalPos " + finalPos.gameObject.name + " mandala=" + finalPos.mandala);
        game.DoneMoving(finalPos.mandala);
    }

    // OnMouseDown is called when the piece is clicked or tapped on
    void OnMouseDown() {

        if (playable && !game.Paused()) {
            Move(game.dice.rollValue);
        }
    }
}
