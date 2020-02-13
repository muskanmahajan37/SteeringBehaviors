using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSpinner : MonoBehaviour {

    private float directionFacing = 0.0f; // Direction the spinner is facing in radians. 0 rad => facing right (pos x). Always between [0, 2*PI).
    private float rotationalVelo = 0.0f;  // How fast the spinner is currently spinning. Positive => CCW
    private float rotationalAcc = 0.0f;   // How fast the spinner is accelerating it's spin. Positive => CCW
    const float MAX_ROT_VELO = 0.03f;
    const float MAX_ROT_ACCL = 0.001f;

    private Vector2 currentPosition { get { return this.transform.position; }
                                      set { this.transform.position = value; } }
    private Vector2 currentVelocity = new Vector2();
    private Vector2 currentAcceleration = new Vector2();

    const float MAX_LIN_VELO = 0.01f;
    const float MAX_LIN_ACC = 0.0005f; // Careful, too small a value will underflow to 0 when normalizing (small is about 0.0000005f)

    void Start() {
        initCurveDrawers();

        Debug.Log(string.Format("Atan (x1, y1) {0}", Mathf.Atan2(1, 1) * Mathf.Rad2Deg));
        Debug.Log(string.Format("Atan (x1, y-1) {0}", ((2 * Mathf.PI) + Mathf.Atan2(-1, 1)) * Mathf.Rad2Deg));
        Debug.Log(string.Format("Atan (x-1, y1) {0}", Mathf.Atan2(1, -1) * Mathf.Rad2Deg));
        Debug.Log(string.Format("Atan (x-1, y-1) {0}", ((2 * Mathf.PI) + Mathf.Atan2(-1, -1) ) * Mathf.Rad2Deg));

    }

    // Update is called once per frame
    void Update() {

        Vector2 targetPosition = new Vector2(0,0);

        bool positionFacing = true; // TODO better var name
        float targetDirectionFacing = generateTargetDirectionFacing(targetPosition);
        float targetRotVelo = generateTargetRotVelo(targetDirectionFacing);
        if (positionFacing) {
            updateDirectionFacing();
            updateRotVelo();
            updateRotAccPointToVelo(targetRotVelo);
        }

        //Debug.Log("Dir facing: " + directionFacing);


        bool veloTurn = false; // Only velo in this direction facing
        bool accelTurn = true; // Only accel in this direction facing
        bool instantTurn = false;
        if (instantTurn) {
            Vector2 targetVelocity = generateTargetVelo(targetPosition);

            updatePosition();
            updateVelocity();

            bool accelPointToVelo = true;
            if (accelPointToVelo) {
                updateAccelPointToVelo(targetVelocity);
            } // Good implimentation
            else {
                updateAccelPointToPos(targetPosition);
            }  // Spins out of control

        } else if (veloTurn){
            // Else we must velo in the direction we are facing

            updatePosition();
            updateVelocity(targetDirectionFacing);

            // Velocity is a % of how close we are to facing the target direction

        } else if (accelTurn) {
            updatePosition();
            updateVelocity();
            updateAccelPointToCurrentDir(targetDirectionFacing);
        }


       


        bool showVelocity = true;
        if (showVelocity) { drawVelocityLine(); }

        bool showAccel = true;
        if (showAccel) { drawAccelLine(); }


        bool showDir = true;
        if (showDir) {
            drawDirectionLine();
        }

        bool showRotVelo = true;
        if (showRotVelo) {
            drawRotVeloCurve();
        }

        bool showRotAccl = true;
        if (showRotAccl) {
            drawRotAcclCurve();
        }
        /**
         * TODO: other see strategies
         * 
         * 2) Impliment the use of rotational acceleration instead of "buzzing" turn on a dime of only linear accelerations
         * 2.1) Target acceleration is proportional with the direction facing. If looking straight at target target accel is max accel
         * 2.2) The sum of (Mag_lin_acc + rot_acc) = constant. Maybe perlin noise it to shift between or proportional to how close to target velo
         * 3) Acceleration is always max_accel
         */

    }

    ///////////////////////////////////
    // Line drawers

    private int previousVelocityLineHash = LineDrawer.ILLEGAL_LINE_HASH;
    private void drawVelocityLine() {
        LineDrawer.RemoveLine(previousVelocityLineHash);
        float percentTowardsMax = this.currentVelocity.magnitude / MAX_LIN_VELO;
        Vector2 endPt = this.currentPosition + (this.currentVelocity.normalized * percentTowardsMax);
        previousVelocityLineHash = LineDrawer.AddLine(this.transform.position, endPt);
    }

    private int previousAccelerationLineHash = LineDrawer.ILLEGAL_LINE_HASH;
    private void drawAccelLine() {
        LineDrawer.RemoveLine(previousAccelerationLineHash);
        float percentTowardsMax = this.currentAcceleration.magnitude / MAX_LIN_ACC;

        // Bump the acceleration line so not to block the velocity line
        Vector2 bump = new Vector2(0.01f, 0.01f); 
        Vector2 endPt = this.currentPosition + (this.currentAcceleration.normalized * percentTowardsMax ) + bump;
        Vector2 startPt = this.currentPosition + bump;

        previousAccelerationLineHash = LineDrawer.AddLine(startPt, endPt, Color.red);
    }

    // Line drawers
    ///////////////////////////////////
    // Linear acceleration

    private Vector2 generateTargetVelo(Vector2 targetPosition) {
        Vector2 targetVelocity = (targetPosition - this.currentPosition);
        capSize(ref targetVelocity, MAX_LIN_VELO);
        return targetVelocity;
    }

    private void updatePosition() {
        // This is strange looking because the current position is owned by this.transform.position which
        // requies a whole new vector to change. 
        Vector2 newPosition = currentPosition;
        newPosition += currentVelocity;
        currentPosition = newPosition;
    }

    private void updateVelocity() {
        this.currentVelocity += currentAcceleration;
        capSize(ref this.currentVelocity, MAX_LIN_VELO);
    }

    private void updateVelocity(float targetDirectionFacing) {
        // This velocity is will always point in the direction facing
        // The magnitude is a % between [0, Max Velo] based on how close the targetDirectionFacing
        //  is to this actual direction facing

        this.currentVelocity = new Vector2(Mathf.Cos(directionFacing), Mathf.Sin(directionFacing));
        float acuteAngle = Mathf.Abs(findAcute(this.directionFacing, targetDirectionFacing));
        float percentFacingTarget = (Mathf.PI - acuteAngle) / Mathf.PI;
        float magnitude = percentFacingTarget * MAX_LIN_VELO;
        this.currentVelocity *= magnitude;
    }

    private void updateAccelPointToVelo(Vector2 targetVelocity) {
        // The new acceleration simply tries to convert the current velocity into the target velocity
        this.currentAcceleration = targetVelocity - this.currentVelocity;
        capSize(ref this.currentAcceleration, MAX_LIN_ACC);
    }

    private void updateAccelPointToPos(Vector2 targetPosition) {
        // The new acceleration points towards the target position directly
        // Notice how slight discrepencies in float arithmetic causes the object to spin out of control
        // Reccomended to used updateAccelPointToVelo(targetVelo);
        this.currentAcceleration = targetPosition - this.currentPosition;
        capSize(ref this.currentAcceleration, MAX_LIN_ACC);
    }

    private void updateAccelPointToCurrentDir(float targetDirectionFacing) {
        this.currentAcceleration = new Vector2(Mathf.Cos(directionFacing), Mathf.Sin(directionFacing));
        float acuteAngle = Mathf.Abs(findAcute(this.directionFacing, targetDirectionFacing));
        float percentFacingTarget = (Mathf.PI - acuteAngle) / Mathf.PI;
        float magnitude = percentFacingTarget * MAX_LIN_ACC;
        this.currentAcceleration *= magnitude;
    }

    // Linear acceleration
    ///////////////////////////////////
    // Curve drawers
    const int numEdges = 20;


    private void initCurveDrawers() {
        this.velocityCurveDrawer = this.transform.GetChild(0).GetComponent<CurveDrawer>(); // TODO make this cleaner
        this.accelCurveDrawer = this.transform.GetChild(1).GetComponent<CurveDrawer>(); // TODO make this cleaner
    }

    private int previousDirLineHash = LineDrawer.ILLEGAL_LINE_HASH;
    private void drawDirectionLine() {
        LineDrawer.RemoveLine(previousDirLineHash);
        Vector2 endPt = this.currentPosition;

        float radius = 0.3f;
        endPt.x += Mathf.Cos(directionFacing) * radius;
        endPt.y += Mathf.Sin(directionFacing) * radius;
        previousDirLineHash = LineDrawer.AddLine(this.currentPosition, endPt, Color.black);
    }

    private CurveDrawer velocityCurveDrawer;
    private void drawRotVeloCurve() {
        float percentTowardsMax = this.rotationalVelo / MAX_ROT_VELO;
        float endAngle = directionFacing + (percentTowardsMax * (Mathf.PI / 2));
        velocityCurveDrawer.drawCurve(numEdges, directionFacing, endAngle, 0.3f);
    }

    private CurveDrawer accelCurveDrawer;
    private void drawRotAcclCurve() {
        float percentTowardsMax = this.rotationalAcc / MAX_ROT_ACCL;
        float endAngle = directionFacing + (percentTowardsMax * (Mathf.PI / 2));
        accelCurveDrawer.drawCurve(numEdges, directionFacing, endAngle, 0.2f);

    }

    // Curve drawers
    ///////////////////////////////////
    // Rotational acceleration

    private float generateTargetDirectionFacing(Vector2 targetPosition) {
        Vector2 deltaV = targetPosition - this.currentPosition;
        float result = Mathf.Atan2(deltaV.y, deltaV.x);
        if (deltaV.y < 0) { result = (2 * Mathf.PI) + result; }
        return result;
    }

    private float generateTargetRotVelo(float targetDirFacing) {
        float acuteAngle = findAcute(this.directionFacing, targetDirFacing);
        return Mathf.Clamp(acuteAngle, -MAX_LIN_VELO, MAX_LIN_VELO);
    }

    private void updateDirectionFacing() {
        this.directionFacing += this.rotationalVelo;

        if (directionFacing < 0)                 { directionFacing += 2 * Mathf.PI; }
        else if (directionFacing > 2 * Mathf.PI) { directionFacing -= 2 * Mathf.PI; }
    }

    private void updateRotVelo() {
        this.rotationalVelo += this.rotationalAcc;
        this.rotationalVelo = Mathf.Clamp(this.rotationalVelo, -MAX_ROT_VELO, MAX_ROT_VELO);
    }


    private void updateRotAccPointToVelo(float targetRotVelo) {
        this.rotationalAcc = targetRotVelo - this.rotationalVelo;
        this.rotationalAcc = Mathf.Clamp(this.rotationalAcc, -MAX_ROT_ACCL, MAX_ROT_ACCL);
    }


    // Rotational Acceleration
    ///////////////////////////////////////////////////////////
    // Utility

    private static Vector2 radianToUnitDirection(float radians) {
        // Converts the given angle (in radians) to a unit vector
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }


    private static void capSize(ref Vector2 v, float maxSize) {
        if (v.magnitude > maxSize) {
            v.Normalize();
            v *= maxSize;
        }
    }

    private static float findAcute(float sourceAngle, float targetAngle) {
        // Return the smallest angle between source and target. Results will be between [-Pi, Pi]
        // Negative result => source -> target transformation is a CW rotation
        float acuteAngle = targetAngle - sourceAngle;
        if      (acuteAngle > Mathf.PI) { acuteAngle = acuteAngle - (2 * Mathf.PI); }
        else if (acuteAngle < -Mathf.PI) { acuteAngle = (2 * Mathf.PI) + acuteAngle; }
        return acuteAngle;
    }
}
