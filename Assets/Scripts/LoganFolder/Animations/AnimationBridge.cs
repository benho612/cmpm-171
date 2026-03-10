using UnityEngine;

public class AnimationBridge : MonoBehaviour
{
    private Animator _animator;
    private MovementSandBox _movement;
    private CharacterController _controller;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _movement = GetComponent<MovementSandBox>();
        _controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(_controller.velocity);

        float moveX = localVelocity.x / _movement.walkSpeed;
        float moveZ = localVelocity.z / _movement.walkSpeed;

        //give vars to animator
        _animator.SetFloat("MoveX", moveX, 0.1f, Time.deltaTime);
        _animator.SetFloat("MoveZ", moveZ, 0.1f, Time.deltaTime);
        float currentSpeed = _controller.velocity.magnitude;
        _animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }

    public void PlayAttack(string moveName){
        _animator.CrossFadeInFixedTime(moveName, 0.15f);
    }
    public void PlayBlock(float animPoint){
        _animator.Play("Standing Block", 1, animPoint);
    }
    public void StopBlock(){
        _animator.Play("EmptyState", 1, 0f);
    }
    public void BackToLocomotion(){
        _animator.Play("Locomotion", 0, 0f);
    }
    public void HitStopAnim(){
        _animator.speed = 0;
    }
    public void ResumeAnim(){
        _animator.speed = 1;
    }
}
