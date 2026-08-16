using UnityEngine;
using System.Collections;
public class PlayerMovement : MonoBehaviour
{
    public LayerMask obstacleLayer;
    bool isMoving = false;
    Vector2 input;
    Vector2 targetPosition;
    bool isRunning = false;
    float moveDuration = 0.1f;
    public Vector2 facingDirection {get; private set;} = Vector2.down;
    void Update()
    {
        if(!isMoving) GetInput();
        if(isRunning == true) moveDuration = 0.01f;
        else moveDuration = 0.05f;
    }
    void GetInput()
    {
        input = Vector2.zero;
        if(Input.GetKey(KeyCode.UpArrow)) input = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)) input = Vector2.down;
        else if (Input.GetKey(KeyCode.LeftArrow)) input = Vector2.left;
        else if (Input.GetKey(KeyCode.RightArrow)) input = Vector2.right;
   if (input != Vector2.zero) {facingDirection = input;StartMoving(); }
   if (!isRunning && Input.GetKeyDown(KeyCode.LeftShift)) isRunning = true;
   else if (isRunning && Input.GetKeyDown(KeyCode.LeftShift)) isRunning = false;
    }
    void StartMoving()
    {
        targetPosition = (Vector2)transform.position + input;
        if (!Physics2D.Raycast(transform.position, input, 1f, obstacleLayer))
        {
            StartCoroutine(Move());
        }
    }
    IEnumerator Move()
    {
        isMoving = true;
        Vector2 startPosition = transform.position;
        float elapsedTime = 0f;
        while(elapsedTime < moveDuration)
        {
            transform.position = Vector2.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
        isMoving = false;
    }
}