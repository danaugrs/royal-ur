using UnityEngine;
using UnityEngine.UI;

public class CameraGimbal : MonoBehaviour
{
    public Game game;
    Gyroscope m_Gyro;

    public bool overBoard = false;
    private bool settled = true;
    private float timer = 0;

    public AnimationCurve slideCurve;
    public AnimationCurve tiltCurve;
    public Vector3 posLeft;
    public Vector3 posRight;
    public float duration;

    void Start() {
        //Set up and enable the gyroscope (check your device has one)
        m_Gyro = Input.gyro;
        m_Gyro.enabled = true;
    }

    private static Quaternion GyroToUnity(Quaternion q) {

        return new Quaternion(q.x, q.z, q.y, -q.w);
    }

    void ApplyGyroTilt() {

        Quaternion q = GyroToUnity(Input.gyro.attitude);
        q.Normalize();

        Quaternion swing = Quaternion.identity;
        Quaternion twist = Quaternion.identity;
        Util.SwingTwistDecomposition(q, Vector3.up, out swing, out twist);

        Vector3 swingEuler = swing.eulerAngles;
        //swingEuler.x = -swingEuler.x;
        //swingEuler.z = -swingEuler.z;
        swing = Quaternion.Euler(swingEuler);

        float angle = 0.0f;
        Vector3 swingAxis = Vector3.zero;
        swing.ToAngleAxis(out angle, out swingAxis);

        swingAxis = Quaternion.Inverse(twist) * swingAxis;
        Quaternion final = Quaternion.AngleAxis(angle, swingAxis);

        // Decompose final rotation into X and Y components
        Quaternion swingX = Quaternion.identity;
        Quaternion swingY = Quaternion.identity;
        Util.SwingTwistDecomposition(final, Vector3.right, out swingX, out swingY);

        /*
        float swingYangle = 0.0f;
        Vector3 swingYAxis = Vector3.zero;
        swingY.ToAngleAxis(out swingYangle, out swingYAxis);
        Debug.Log(swingYangle + " " + swingYAxis);

        if (swingYangle > 90) {
            swingYangle = 90;
        }
        if (swingYangle > 180) {
            swingYangle = 0;
        }

        Quaternion yNinety = Quaternion.AngleAxis(90, swingYAxis);
        */

        transform.rotation = Quaternion.Slerp(Quaternion.identity, swingX, 0.1f);// * tiltCurve.Evaluate(angle/ 180)); // TODO Apply animation curve here with limit
        transform.rotation = Quaternion.Slerp(transform.rotation, swingY, 0.2f);// * tiltCurve.Evaluate(angle/ 180)); // TODO Apply animation curve here with limit
        //transform.rotation = Quaternion.Slerp(transform.rotation, yNinety, 0.3f * swingYangle / 90);// * tiltCurve.Evaluate(angle/ 180)); // TODO Apply animation curve here with limit
    }

    void Update() {

        ApplyGyroTilt();        

        if (!settled) {
            float val = slideCurve.Evaluate(timer / duration); ;
            if (overBoard) {
                transform.position = Vector3.Lerp(posLeft, posRight, val);
            } else {
                transform.position = Vector3.Lerp(posRight, posLeft, val);
            }
            if (val >= 1.0f) {
                settled = true;
                timer = 0;
                if (!overBoard) { // Game has been abandoned
                    game.ResetGame();
                    if (game.backPanel.activeSelf) {
                        game.backPanel.SetActive(false);
                        game.ToggleBackPanel();
                    } else if (game.winPanel.activeSelf) {
                        game.winPanel.SetActive(false);
                        game.ToggleWinPanel();
                    }
                }
            }
            timer += Time.deltaTime;
        }
    }

    public void Slide() {
        overBoard = !overBoard;
        settled = false;
    }

}