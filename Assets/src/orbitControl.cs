using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class orbitControl : MonoBehaviour
{
    
    public Transform cameraControl;
    
    /* Mouse Buttons */
    private const int MIDDLE_MOUSE = 2;
    private const int RIGHT_MOUSE = 1;
    private const int LEFT_MOUSE = 0;

    /* Translate Sensitivities */
    public float sensitivityX = 20.0f;
    public float sensitivityY = 20.0f;
    public float sensitivityUP = 20.0f;

    /* Rotation Sensitivies */
    public float rotationSensitivityX = 100.0f;
    public float rotationSensitivityY = 100.0f;

    // Track of rotation
    float rotationX = 0.0f;
    float rotationY = 90.0f;

    public float BoundDistance = 50.0f;
    public float[] InitialCoordinates;
    public bool ENABLED = true;
    

    void Start()
    {
        this.rotationX = cameraControl.localRotation.eulerAngles.x;
        this.rotationY = cameraControl.localRotation.eulerAngles.y;
        Vector3 InitialPosition = cameraControl.transform.position;
        InitialCoordinates = new float[] {
            cameraControl.transform.position.x,
            cameraControl.transform.position.y,
            cameraControl.transform.position.z};
        
    }

    public void update_rotation() {
        this.rotationX = cameraControl.localRotation.eulerAngles.x;
        this.rotationY = cameraControl.localRotation.eulerAngles.y;
    }

    Vector3 UnitVector(int axis) {
        switch (axis) {
            case 0:
                return new Vector3(1, 0, 0);
            case 1:
                return new Vector3(0, 1, 0);
            case 2:
                return new Vector3(0, 0, 1);
            default:
                return new Vector3(0, 0, 0);
        }
    }

    

    float Distance1d(float x_0, float x_1) {
        return Mathf.Abs(x_0 - x_1);
    }

    void KeepInBounds() {
        float[] cords = new float[3] {
            cameraControl.transform.position.x,
            cameraControl.transform.position.y,
            cameraControl.transform.position.z
        };


        for (int i=0; i < 3; i++) {
            if (cords[i] > InitialCoordinates[i] + BoundDistance) {
                float diff = this.Distance1d(cords[i], (InitialCoordinates[i] + BoundDistance));
                cameraControl.transform.position -= diff * UnitVector(i);
            } 
            else if (cords[i] < InitialCoordinates[i] - BoundDistance) {
                float diff = this.Distance1d(cords[i], InitialCoordinates[i] - BoundDistance); 
                cameraControl.transform.position += diff * UnitVector(i);
            }
        }
    }


    void Update()
    {
        if (!ENABLED) 
            return;

        KeepInBounds();
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float deltaY = Input.mouseScrollDelta.y;

        // Move arround in local axis
        if (Input.GetMouseButton(MIDDLE_MOUSE)) {

            cameraControl.transform.Translate(
                Vector3.left * mouseX * sensitivityX * Time.deltaTime 
            );

            cameraControl.transform.Translate(
                -Vector3.forward * mouseY * sensitivityY * Time.deltaTime
            );
        };

        // Move upwards
        if (Input.GetMouseButton(RIGHT_MOUSE)) {
            cameraControl.transform.Translate(
                -Vector3.up * mouseY * sensitivityUP * Time.deltaTime
            );
        };

        // Rotate
        if (Input.GetMouseButton(LEFT_MOUSE)) {
            
            // Upward Rotation
            rotationX += mouseY * rotationSensitivityY * Time.deltaTime;
            rotationX = Mathf.Clamp(rotationX, -90.0f, 90.0f);

            // Sideways
            rotationY += -mouseX * rotationSensitivityX * Time.deltaTime;

            cameraControl.transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0);
        };

        // Zoom
        if (deltaY > 0) {
            cameraControl.transform.Translate(Vector3.forward * 2);
        }
        else if (deltaY < 0) {
            cameraControl.transform.Translate(-Vector3.forward * 2);
        }
    }
}
