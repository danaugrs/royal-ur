using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameType { SinglePlayer, Multiplayer };
public enum GameMode { Corridor, LoopAround };
public enum GameState { Menu, Sliding, AwaitingRoll, Rolling, AwaitingMove, GameOver, StartingReplay };
public enum Player { One, Two }

public class Game : MonoBehaviour {

    public GameObject Positions;
    public GameObject Player1Pieces;
    public GameObject Player2Pieces;

    public Camera Camera;
    public CameraGimbal Cam;
    float lowestAspect = 1.3333333333f;
    float highestAspect = 1.7777777777f;
    float camPosYhigh = 2.8f;
    float camPosYlow = 2.2f;

    public Dice dice;
    public Material laurelLeft;
    public Material laurelRight;
    public Color laurelBlue;
    public Color laurelRed;

    // Back and win panels
    public AnimationCurve panelSlideCurve;
    public GameObject backPanel;
    public GameObject winPanel;
    bool backPanelIn = false;
    bool winPanelIn = false;
    bool backPanelSettled = true;
    bool winPanelSettled = true;
    public float panelSlideDuration;
    private float backPanelSlideTimer = 0;
    private float winPanelSlideTimer = 0;
    public Vector3 posLeft;
    public Vector3 posGame;
    public Vector3 posRight;

    public GameState state = GameState.Menu;
    public GameType type = GameType.SinglePlayer;
    public GameMode mode = GameMode.Corridor;
    public Player player = Player.One;
    public Player startingPlayer = Player.One;
    bool firstPlay = true;

    public AudioSource moveSound;
    public AudioSource mandalaSound;
    public AudioSource captureSound;
    public AudioSource zeroThrowSound;
    public AudioSource noMovesSound;
    public AudioSource diceSound;
    public AudioSource slideSound;
    public AudioSource winSound;
    public AudioSource replaySound;
    public AudioSource playSound;

    public Color diceColorBlue = new Color(0, 0.175f, 1);
    public Color diceColorRed = new Color(1, 0, 0);

    public float computerDiceRollDelay;
    public float computerMoveDelay;
    float delayTime = 0;
    Piece compPieceToMove = null;

    // Swipe
    private Vector3 fp;   // First touch position
    private Vector3 lp;   // Last touch position
    //private Vector3 origPos;   // Original panel pos
    private float dragDistance;  // Minimum distance for a swipe to be registered

    // Start is called in the very beginning
    void Start () {

        UpdateCameraHeight();

        dragDistance = Screen.height * 25 / 100; // dragDistance is 15% height of the screen

        backPanelSlideTimer = panelSlideDuration;
        winPanelSlideTimer = panelSlideDuration;

        SetCorridorMode();
    }

    // UpdateCameraHeight moves the camera up and down to allow the entire menu/game to be visible in all supported aspect ratios
    void UpdateCameraHeight() {
        // aspect ratio  | camera pos Y
        // 4:3   = 1.333 | 2.8Y
        // 16:9  = 1.777 | 2.2Y
        Debug.Log("Camera aspect ratio: " + Camera.aspect);
        float yPos = camPosYlow;
        if (Camera.aspect < lowestAspect) {
            yPos = camPosYhigh;
        } else if (Camera.aspect > highestAspect) {
            yPos = camPosYlow;
        } else {
            // Interpolate between those values
            float prop = (Camera.aspect - lowestAspect) / (highestAspect - lowestAspect);
            yPos = camPosYlow + (1 - prop) * (camPosYhigh - camPosYlow);
        }
        Camera.transform.position = new Vector3(Camera.transform.position.x, yPos, Camera.transform.position.z);
    }

    // Toggle Show/Hide winPanel
    public void ToggleWinPanel() {

        if (winPanelSettled) {
            Debug.Log("Toggling Win Panel");
            winPanelIn = !winPanelIn;
            winPanelSettled = false;
            if (winPanelIn) {
                winPanel.SetActive(true);
                Pause();
            } else {
                UnPause();
            }
            winPanelSettled = false;
            winPanelSlideTimer = panelSlideDuration - winPanelSlideTimer;
            if (winPanel.activeSelf) {
                slideSound.Play();
            }
        }
    }

    // Toggle Show/Hide backPanel
    public void ToggleBackPanel() {

        if (backPanelSettled) {
            Debug.Log("Toggling Back Panel");
            backPanelIn = !backPanelIn;
            if (backPanelIn) {
                backPanel.SetActive(true);
                Pause();
            } else {
                UnPause();
            }
            backPanelSettled = false;
            backPanelSlideTimer = panelSlideDuration - backPanelSlideTimer;
            if (backPanel.activeSelf) {
                slideSound.Play();
            }
        }
    }

    void SetLaurelsColor(Color c) {

        laurelLeft.SetColor("_Color", c);
        laurelRight.SetColor("_Color", c);
    }

    // Update is called once per frame
    void Update() {

        // Check if escape/back was pressed
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Debug.Log("Escape/Back");
            if (state == GameState.Menu) {
                Debug.Log("Quitting game");
                Application.Quit();
            } else if (state == GameState.GameOver) {
                ToggleWinPanel();
            } else { // Game In-progress
                ToggleBackPanel();
            }
        }

        // Update panel positions if necessary
        if (!backPanelSettled) {
            float val = panelSlideCurve.Evaluate(backPanelSlideTimer / panelSlideDuration);
            if (backPanelIn) {
                backPanel.transform.position = Vector3.Lerp(posLeft, posGame, val);
            } else {
                backPanel.transform.position = Vector3.Lerp(posGame, posLeft, val);
            }
            if (val == 1.0f) {
                backPanelSettled = true;
                backPanelSlideTimer = panelSlideDuration;
                if (!backPanelIn) {
                    backPanel.SetActive(false);
                }
            }
            backPanelSlideTimer += Time.deltaTime;
        }
        if (!winPanelSettled) {
            float val = panelSlideCurve.Evaluate(winPanelSlideTimer / panelSlideDuration);
            if (winPanelIn) {
                winPanel.transform.position = Vector3.Lerp(posRight, posGame, val);
            } else {
                winPanel.transform.position = Vector3.Lerp(posGame, posRight, val);
            }
            if (val == 1.0f) {
                winPanelSettled = true;
                winPanelSlideTimer = panelSlideDuration;
                if (!winPanelIn) {
                    winPanel.SetActive(false);
                }
            }
            winPanelSlideTimer += Time.deltaTime;
        }

        // Check if we are due for a computer roll/move
        if (delayTime > 0 && !Paused()) {
            delayTime -= Time.deltaTime;
            if (delayTime <= 0) {
                delayTime = 0;
                if (compPieceToMove != null) { // We were waiting for the computer to move
                    compPieceToMove.Move(dice.rollValue);
                    compPieceToMove = null;
                } else { // We were waiting for the computer to roll the dice
                    dice.Roll();
                }
            }
        }

        // Check for swipe
        if (state != GameState.Menu && Input.touchCount == 1) { // user is touching the screen with a single touch
            Touch touch = Input.GetTouch(0); // get the touch
            if (touch.phase == TouchPhase.Began) { //check for the first touch
                fp = touch.position;
                lp = touch.position;
                /*if (state == GameState.GameOver) {
                    origPos = winPanel.transform.position;
                } else {
                    origPos = backPanel.transform.position;
                }*/
            } else if (touch.phase == TouchPhase.Moved) { // update the last position based on where they moved
                lp = touch.position;
                //(Mathf.Abs(lp.x - fp.x) > dragDistance && 
                /*if (Mathf.Abs(lp.x - fp.x) > Mathf.Abs(lp.y - fp.y)) {
                    float diffX = lp.x - fp.x;
                    diffX /= 100;
                    if (state == GameState.GameOver) {

                    } else {
                        backPanel.active = true;
                        backPanel.transform.position = new Vector3(Mathf.Clamp(diffX + origPos.x, -6, 0), 0, 0);
                    }
                } */
            } else if (touch.phase == TouchPhase.Ended) { //check if the finger is removed from the screen
                lp = touch.position;  //last touch position. Ommitted if you use list
                //Check if drag distance is greater than minimum
                if (Mathf.Abs(lp.x - fp.x) > dragDistance || Mathf.Abs(lp.y - fp.y) > dragDistance) { // It's a drag
                    //check if the drag is vertical or horizontal
                    if (Mathf.Abs(lp.x - fp.x) > Mathf.Abs(lp.y - fp.y)) { // If the horizontal movement is greater than the vertical movement...
                        if ((lp.x > fp.x)) { // Right swipe
                            Debug.Log("Right Swipe");
                            if (state == GameState.GameOver && winPanelIn) {
                                ToggleWinPanel();
                            } else if (state != GameState.GameOver && !backPanelIn) {
                                ToggleBackPanel();
                            }
                        } else { //Left swipe
                            Debug.Log("Left Swipe");
                            if (state == GameState.GameOver && !winPanelIn) {
                                ToggleWinPanel();
                            } else if (state != GameState.GameOver && backPanelIn) {
                                ToggleBackPanel();
                            }
                        }
                    } else {   //the vertical movement is greater than the horizontal movement
                        if (lp.y > fp.y) { // Up swipe
                            Debug.Log("Up Swipe");
                        } else { //Down swipe
                            Debug.Log("Down Swipe");
                        }
                    }
                } else {   //It's a tap as the drag distance is less than 20% of the screen height
                    Debug.Log("Tap");
                }
            }
        }
    }

    public bool Paused() {

        return backPanelIn || winPanelIn;
    }

    void Pause() {

        moveSound.Pause();
        mandalaSound.Pause();
        captureSound.Pause();
        zeroThrowSound.Pause();
        noMovesSound.Pause();
        diceSound.Pause();
    }

    void UnPause() {

        moveSound.UnPause();
        mandalaSound.UnPause();
        captureSound.UnPause();
        zeroThrowSound.UnPause();
        noMovesSound.UnPause();
        diceSound.UnPause();
    }

    void CancelSounds() {

        moveSound.Stop();
        mandalaSound.Stop();
        captureSound.Stop();
        zeroThrowSound.Stop();
        noMovesSound.Stop();
        diceSound.Stop();
    }

    public void Replay() {

        replaySound.Play();

        state = GameState.StartingReplay;
        ResetGame();
        ToggleWinPanel();
        StartGame();
    }

    public void ExitToMenu() {

        // Set state to Menu
        state = GameState.Menu;

        // Clear starting player
        firstPlay = true;

        // Cancel any scheduled computer roll or move
        delayTime = 0;
        compPieceToMove = null;

        // Cancel game sounds
        CancelSounds();

        // Slide the camera, which will reset the game once it's on top of the menu
        Cam.Slide();
    }

    public void ResetGame() {

        Debug.Log("Reset Game");

        // Cancel any scheduled computer roll or move
        delayTime = 0;
        compPieceToMove = null;

        // Cancel game sounds
        CancelSounds();

        // Reset dice and pieces
        dice.Reset();
        MarkPiecesAsNotPlayable();
        ResetBoard();
    }

    public void StartGame() {


        Debug.Log("Starting game...");

        if (state != GameState.Menu && state != GameState.StartingReplay) {
            Debug.Log("Can't start new game from current state");
            return;
        }

        // Set starting player randomly if first game, else alternate
        if (firstPlay == true) {
            Debug.Log("First play!");
            playSound.Play();
            if (Random.value > 0.5) {
                startingPlayer = Player.One;
                player = Player.One;
            } else {
                startingPlayer = Player.Two;
                player = Player.Two;
            }
            firstPlay = false;
        } else {
            Debug.Log("Replay!");
            if (startingPlayer == Player.One) {
                startingPlayer = Player.Two;
                player = Player.Two;
            } else { // Player.Two
                startingPlayer = Player.One;
                player = Player.One;
            }
        }


        Debug.Log("Player = " + player);

        if (!Cam.overBoard) {
            state = GameState.Sliding;
            Cam.Invoke("Slide", 0.5f);
            Invoke("SetAwaitingRoll", 1.5f);
        } else { // this is a replay - no need for delays
            SetAwaitingRoll();
        }
    }

    public void Roll() {

        Debug.Log("Roll End (game.Roll())");
        if (state != GameState.Menu && state != GameState.StartingReplay) {
            if (dice.rollValue > 0) {
                SetAwaitingMove();
            } else {
                zeroThrowSound.Play();
                SwitchPlayerTurn();
                SetAwaitingRoll();
            }
        }
    }

    void SetAwaitingRoll() {

        Debug.Log("AwaitingRoll Player=" + player.ToString());

        if (ComputersTurn()) {
            Debug.Log("Computer's turn");
            // Then roll and don't bother to switch state
            //dice.Invoke("Roll", computerDiceRollDelay);
            delayTime = computerDiceRollDelay;
            return;
        }

        state = GameState.AwaitingRoll;

        // Change dice glow color
        if (player == Player.One) {
            dice.SetGlowColor(diceColorBlue);
        } else { // Player.Two
            dice.SetGlowColor(diceColorRed);
        }

    }

    void SetAwaitingMove() {

        Debug.Log("AwaitingMove Player=" + player.ToString());

        int playablePieces = 0;

        // Highlight playable pieces by a human player
        if (player == Player.One) {
            foreach (Transform player1piece in Player1Pieces.transform) {
                Piece piece = player1piece.gameObject.GetComponent<Piece>();
                bool canMove = piece.CanMove(dice.rollValue);
                piece.SetPlayable(canMove);
                if (canMove) {
                    playablePieces += 1;
                }
            }
        } else {
            foreach (Transform player2piece in Player2Pieces.transform) {
                Piece piece = player2piece.gameObject.GetComponent<Piece>();
                bool canMove = piece.CanMove(dice.rollValue);
                if (type == GameType.Multiplayer) {
                    piece.SetPlayable(canMove);
                }
                if (canMove) {
                    playablePieces += 1;
                }
            }
        }

        if (playablePieces > 0) {
            if (ComputersTurn()) {
                ComputerMove();
            } else {
                state = GameState.AwaitingMove;
            }
        } else {
            Debug.Log("NO MOVES AVAILABLE");
            noMovesSound.Play();
            DoneMoving(false);
        }
    }

    bool ComputersTurn() {

        return type == GameType.SinglePlayer && player == Player.Two;
    }

    // This method encapsulates a simple rule-based AI
    void ComputerMove() {

        // Get list of playable pieces along with their final positions if moved
        List<Piece> playableCompPieces = new List<Piece>();
        List<Position> finalPositions = new List<Position>();
        foreach (Transform player2piece in Player2Pieces.transform) {
            Piece piece = player2piece.gameObject.GetComponent<Piece>();
            Position finalPos = piece.FinalPos(dice.rollValue);
            bool canMove = piece.PosAvailable(finalPos);
            if (canMove) {
                playableCompPieces.Add(piece);
                finalPositions.Add(finalPos);
            }
        }

        // Compile mandala and capture information
        List<int> mandalaIndexes = new List<int>();
        List<int> captureIndexes = new List<int>();
        List<string> mandalaNames = new List<string>();
        for (int i = 0; i < finalPositions.Count; i++) {
            if (finalPositions[i].mandala) {
                mandalaIndexes.Add(i);
                mandalaNames.Add(finalPositions[i].name);
            }
            if (finalPositions[i].piece != null && finalPositions[i].piece.player != player) {
                captureIndexes.Add(i);
            }
        }

        // If mandala(s) is/are available - go to one (if more then one is available then the priority is MIDDLE -> LAST -> FIRST).
        delayTime = computerMoveDelay;
        if (mandalaIndexes.Count > 0) {
            if (mandalaIndexes.Count == 1) {
                //playableCompPieces[mandalaIndexes[0]].Invoke("Move", computerMoveDelay);
                compPieceToMove = playableCompPieces[mandalaIndexes[0]];
                return;
            } else {
                int middleIndex = mandalaNames.IndexOf("Corridor4");

                int lastIndex;
                if (player == Player.One) {
                    lastIndex = mandalaNames.IndexOf("Player1Last1");
                } else { // Player.Two
                    lastIndex = mandalaNames.IndexOf("Player2Last1");
                }

                int firstIndex;
                if (player == Player.One) {
                    firstIndex = mandalaNames.IndexOf("Player1Pos4");
                } else { // Player.Two
                    firstIndex = mandalaNames.IndexOf("Player2Pos4");
                }

                if (middleIndex >= 0) {
                    //playableCompPieces[mandalaIndexes[middleIndex]].Invoke("Move", computerMoveDelay);
                    compPieceToMove = playableCompPieces[mandalaIndexes[middleIndex]];
                    return;
                } else if (lastIndex >= 0) {
                    //playableCompPieces[mandalaIndexes[lastIndex]].Invoke("Move", computerMoveDelay);
                    compPieceToMove = playableCompPieces[mandalaIndexes[lastIndex]];
                    return;
                } else if (firstIndex >= 0) {
                    //playableCompPieces[mandalaIndexes[firstIndex]].Invoke("Move", computerMoveDelay);
                    compPieceToMove = playableCompPieces[mandalaIndexes[firstIndex]];
                    return;
                }
            }
        }

        // Capture if possible. If multiple captures available - capture the piece furthest along the board i.e. closest to the end.
        if (captureIndexes.Count > 0) {
            int shortesDistToEndIndex = 0;
            int shortesDistToEnd = 100;
            for (int i = 0; i < captureIndexes.Count; i++) {
                int dist = finalPositions[captureIndexes[i]].piece.DistanceToEnd();
                if (dist < shortesDistToEnd) {
                    shortesDistToEnd = dist;
                    shortesDistToEndIndex = i;
                }
            }
            //playableCompPieces[captureIndexes[shortesDistToEndIndex]].Invoke("Move", computerMoveDelay);
            compPieceToMove = playableCompPieces[captureIndexes[shortesDistToEndIndex]];
            return;
        }

        // Play random piece
        int randomIndex = Random.Range(0, playableCompPieces.Count);
        //playableCompPieces[randomIndex].Invoke("Move", computerMoveDelay);
        compPieceToMove = playableCompPieces[randomIndex];

    }


    // SwitchPlayerTurn switches the current player
    void SwitchPlayerTurn() {
        
        if (player == Player.One) {
            player = Player.Two;
        } else {
            player = Player.One;
        }
    }

    public void DoneMoving(bool playAgain) {

        MarkPiecesAsNotPlayable();

        if (state == GameState.GameOver) {
            GameOver();
        } else {
            if (!playAgain) {
                SwitchPlayerTurn();
            } else {
                mandalaSound.Play();
                Debug.Log("MANDALA!");
            }
            SetAwaitingRoll();
        }
    }

    // Mark all pieces as not playable
    void MarkPiecesAsNotPlayable() {
     
        foreach (Transform player1piece in Player1Pieces.transform) {
            player1piece.gameObject.GetComponent<Piece>().SetPlayable(false);
        }
        foreach (Transform player2piece in Player2Pieces.transform) {
            player2piece.gameObject.GetComponent<Piece>().SetPlayable(false);
        }
    }    

    void GameOver() {

        Debug.Log("Game Over! winner = " + player.ToString());

        winSound.Play();
        if (player == Player.One) {
            SetLaurelsColor(laurelBlue);
        } else { // Player.Two
            SetLaurelsColor(laurelRed);
        }
        ToggleWinPanel();
    }

    public void SetSinglePlayer() {

        type = GameType.SinglePlayer;
    }

    public void SetMultiPlayer() {

        type = GameType.Multiplayer;
    }

    /*
     * BOARD
     */

    // Set piece property on start positions and move pieces there
    // Also set position property on pieces
    void ResetBoard() {

        // Clear piece property on all positions
        foreach (Transform pos in Positions.transform) {
            pos.gameObject.GetComponent<Position>().piece = null;
        }

        // Player1 Pieces and Starts
        GetPos("Player1Start1").piece = GetPlayer1Piece(1);
        GetPlayer1Piece(1).position = GetPos("Player1Start1");
        GetPlayer1Piece(1).transform.position = GetPos("Player1Start1").transform.position;
        GetPos("Player1Start2").piece = GetPlayer1Piece(2);
        GetPlayer1Piece(2).position = GetPos("Player1Start2");
        GetPlayer1Piece(2).transform.position = GetPos("Player1Start2").transform.position;
        GetPos("Player1Start3").piece = GetPlayer1Piece(3);
        GetPlayer1Piece(3).position = GetPos("Player1Start3");
        GetPlayer1Piece(3).transform.position = GetPos("Player1Start3").transform.position;
        GetPos("Player1Start4").piece = GetPlayer1Piece(4);
        GetPlayer1Piece(4).position = GetPos("Player1Start4");
        GetPlayer1Piece(4).transform.position = GetPos("Player1Start4").transform.position;
        GetPos("Player1Start5").piece = GetPlayer1Piece(5);
        GetPlayer1Piece(5).position = GetPos("Player1Start5");
        GetPlayer1Piece(5).transform.position = GetPos("Player1Start5").transform.position;
        GetPos("Player1Start6").piece = GetPlayer1Piece(6);
        GetPlayer1Piece(6).position = GetPos("Player1Start6");
        GetPlayer1Piece(6).transform.position = GetPos("Player1Start6").transform.position;
        GetPos("Player1Start7").piece = GetPlayer1Piece(7);
        GetPlayer1Piece(7).position = GetPos("Player1Start7");
        GetPlayer1Piece(7).transform.position = GetPos("Player1Start7").transform.position;

        // Player2 Pieces and Starts
        GetPos("Player2Start1").piece = GetPlayer2Piece(1);
        GetPlayer2Piece(1).position = GetPos("Player2Start1");
        GetPlayer2Piece(1).transform.position = GetPos("Player2Start1").transform.position;
        GetPos("Player2Start2").piece = GetPlayer2Piece(2);
        GetPlayer2Piece(2).position = GetPos("Player2Start2");
        GetPlayer2Piece(2).transform.position = GetPos("Player2Start2").transform.position;
        GetPos("Player2Start3").piece = GetPlayer2Piece(3);
        GetPlayer2Piece(3).position = GetPos("Player2Start3");
        GetPlayer2Piece(3).transform.position = GetPos("Player2Start3").transform.position;
        GetPos("Player2Start4").piece = GetPlayer2Piece(4);
        GetPlayer2Piece(4).position = GetPos("Player2Start4");
        GetPlayer2Piece(4).transform.position = GetPos("Player2Start4").transform.position;
        GetPos("Player2Start5").piece = GetPlayer2Piece(5);
        GetPlayer2Piece(5).position = GetPos("Player2Start5");
        GetPlayer2Piece(5).transform.position = GetPos("Player2Start5").transform.position;
        GetPos("Player2Start6").piece = GetPlayer2Piece(6);
        GetPlayer2Piece(6).position = GetPos("Player2Start6");
        GetPlayer2Piece(6).transform.position = GetPos("Player2Start6").transform.position;
        GetPos("Player2Start7").piece = GetPlayer2Piece(7);
        GetPlayer2Piece(7).position = GetPos("Player2Start7");
        GetPlayer2Piece(7).transform.position = GetPos("Player2Start7").transform.position;

        // Reset end spots
        LinkPlayer1("Player1Last1", "Player1End1");
        LinkPlayer2("Player2Last1", "Player2End1");
    }

    public void UpdateEndSpot(Position oldEndSpot) {

        if (oldEndSpot.nextEndSpot == null) {
            state = GameState.GameOver;
        } else {
            if (player == Player.One) {
                LinkPlayer1("Player1Last1", oldEndSpot.nextEndSpot.name);
            } else { // Player.Two
                LinkPlayer2("Player2Last1", oldEndSpot.nextEndSpot.name);
            }
        }
    }

    public Position LeftmostEmptyStartSpot(Player player) {

        Position leftmost;
        if (player == Player.One) {
            leftmost = GetPos("Player1Start1");
        } else { // Player.Two
            leftmost = GetPos("Player2Start1");
        }
        while (leftmost.piece != null) {
            // if leftmost.nextStartSpot == null then there is no empty start spot (should never happen when this method is called)
            leftmost = leftmost.nextStartSpot;
        }
        return leftmost;
    }

    public void SetCorridorMode() {

        mode = GameMode.Corridor;

        Link("Corridor7", "Corridor8");
        LinkPlayer1("Corridor8", "Player1Last2");
        LinkPlayer2("Corridor8", "Player2Last2");
    }

    public void SetLoopAroundMode() {

        mode = GameMode.LoopAround;

        LinkPlayer1("Corridor7", "Player2Last1");
        LinkPlayer1("Player2Last1", "Player2Last2");
        LinkPlayer1("Player2Last2", "Corridor8");
        LinkPlayer1("Corridor8", "Player1Last2");

        LinkPlayer2("Corridor7", "Player1Last1");
        LinkPlayer2("Player1Last1", "Player1Last2");
        LinkPlayer2("Player1Last2", "Corridor8");
        LinkPlayer2("Corridor8", "Player2Last2");
    }

    private Piece GetPlayer1Piece(int n) {
        return Player1Pieces.transform.Find("Player1Piece" + n).GetComponent<Piece>();
    }

    private Piece GetPlayer2Piece(int n) {
        return Player2Pieces.transform.Find("Player2Piece" + n).GetComponent<Piece>();
    }

    private Position GetPos(string name) {
        return Positions.transform.Find(name).GetComponent<Position>();
    }

    private void Link(string pos1, string pos2) {
        GetPos(pos1).SetNext(GetPos(pos2));
    }

    private void LinkPlayer1(string pos1, string pos2) {
        GetPos(pos1).nextPlayer1Spot = GetPos(pos2);
    }

    private void LinkPlayer2(string pos1, string pos2) {
        GetPos(pos1).nextPlayer2Spot = GetPos(pos2);
    }
}
