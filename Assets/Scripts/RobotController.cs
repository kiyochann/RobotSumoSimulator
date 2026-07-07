using System;
using Unity.Collections;
using UnityEngine;


// ↓リフレクション方式を使用するための準備
[AttributeUsage(AttributeTargets.Method)]
public class RobotCommandAttribute : Attribute{}


public class RobotController : MonoBehaviour
{



    // 方向を指定するための列挙型
    public enum MoveDirection
    {
        F, // 前進 (Forward)
        B, // 後退 (Backward)
        L, // 左旋回 (Left)
        R, // 右旋回 (Right)
    }

    public WheelCollider wheelL;
    public WheelCollider wheelR;
    private Rigidbody rb;

    [Header("Movement (Super Sharp)")]
    public float accelerationTorque = 4000f; // 立ち上がり重視の巨大なトルク
    public float stopBrakeTorque = 8000f;   // 瞬時に止めるブレーキ
    public float maxSpeed = 5f;            // 速度が出すぎないように制限

    // 入力状態を保持する変数
    private float moveInput;
    private float turnInput;
    private bool hasInputThisFrame;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 慣性を最小限にする設定
        rb.linearDamping = 1f;    // 移動の自然減衰
        rb.angularDamping = 10f;  // 回転の自然減衰（旋回がピタッと止まる）

        // 最高速80に対応するための高精度計算設定
        wheelL.ConfigureVehicleSubsteps(1f, 25, 30);
        wheelR.ConfigureVehicleSubsteps(1f, 25, 30);
    }

    void Update()
    {
        // 【使用例】キーボード入力でテストしたい場合はコメントアウトを解除してください
        /*
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        if (v > 0) Move(MoveDirection.F);
        if (v < 0) Move(MoveDirection.B);
        if (h < 0) Move(MoveDirection.L);
        if (h > 0) Move(MoveDirection.R);
        */
    }

    void FixedUpdate()
    {
        // 何らかの入力（Moveの呼び出し）があったかどうか
        if (hasInputThisFrame)
        {
            ApplyMovement(moveInput, turnInput);
        }
        else
        {
            ApplyImmediateStop();
        }

        // 速度制限（加速が鋭いので上限を設ける）
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        //moveInput = 0f;
        //turnInput = 0f;
        //hasInputThisFrame = false;
    }

    private void LateUpdate()
    {
        
    }

    void ApplyMovement(float move, float turn)
    {
        // ブレーキ解除
        wheelL.brakeTorque = 0;
        wheelR.brakeTorque = 0;

        // トルク計算（一気に最大トルクをかける）
        float leftTorque = (move + turn) * accelerationTorque;
        float rightTorque = (move - turn) * accelerationTorque;

        wheelL.motorTorque = leftTorque;
        wheelR.motorTorque = rightTorque;
    }

    void ApplyImmediateStop()
    {
        // トルクをゼロにして強力なブレーキをかける
        wheelL.motorTorque = 0;
        wheelR.motorTorque = 0;
        wheelL.brakeTorque = stopBrakeTorque;
        wheelR.brakeTorque = stopBrakeTorque;

        // 低速になったら物理的に速度をゼロにする（滑り防止）
        if (rb.linearVelocity.magnitude < 2.0f)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }

        // 完全に静止させる
        if (rb.linearVelocity.magnitude < 0.1f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }


    // ↓リフレクション方式を使用したコマンド
    /// <summary>
    /// 外部のスクリプトやAIなどから移動・旋回を指示する関数
    /// </summary>
    /// <param name="direction">進行方向 (F, B, L, R)</param>
    [RobotCommand]
    public void Move(MoveDirection direction)
    {

        hasInputThisFrame = true;

        switch (direction)
        {
            case MoveDirection.F:
                moveInput = 1f;
                turnInput = 0f;
                break;
            case MoveDirection.B:
                moveInput = -1f;
                turnInput = 0f;
                break;
            case MoveDirection.L:
                turnInput = -1f;
                moveInput = 0f;
                break;
            case MoveDirection.R:
                turnInput = 1f;
                moveInput = 0f;
                break;
        }
    }
    

    [RobotCommand]
    public void ClearLock()
    {
        moveInput = 0f;
        turnInput = 0f;
    }

}