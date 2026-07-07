using UnityEngine;

/// <summary>
/// センサーの反応をまとめる「箱（データ構造）」
/// </summary>
public struct SensorData
{
    public bool EnemyLeft;
    public bool EnemyRight;
    public bool LineLeft;
    public bool LineRight;
    public bool StM; // スタートモジュール
}

public class RobotSensorModule : MonoBehaviour
{
    [Header("対物センサー設定 (前方)")]
    [SerializeField] private Transform enemyLeftOrigin;
    [SerializeField] private Transform enemyRightOrigin;
    [SerializeField] private float enemySensorLength = 1.0f;

    [Header("床センサー設定 (下方)")]
    [SerializeField] private Transform lineLeftOrigin;
    [SerializeField] private Transform lineRightOrigin;
    [SerializeField] private float lineSensorLength = 0.1f;

    [Header("システム設定")]
    [Tooltip("インスペクターから現在の信号状態を確認・テストできます")]
    [SerializeField] private bool isStartSignalActive = false;

    /// <summary>
    /// 【プロパティ】外部から安全にアクセス・更新するための窓口
    /// </summary>
    public bool IsStartSignalActive
    {
        get => isStartSignalActive;
        set => isStartSignalActive = value;
    }

    // --- 👇ここから今回追加したイベント駆動ロジック ---

    private void OnEnable()
    {
        // SignalTransmitterが叫んだら、自分の「OnReceiveSignal」関数を実行するように登録する
        SignalTransmitter.OnSignalChanged += OnReceiveSignal;
    }

    private void OnDisable()
    {
        // オブジェクトが消えるときは、メモリリークを防ぐために登録を解除する
        SignalTransmitter.OnSignalChanged -= OnReceiveSignal;
    }

    /// <summary>
    /// SignalTransmitterから一斉送信されたイベントを受け取るハンドラー
    /// </summary>
    private void OnReceiveSignal(bool isActive)
    {
        // 送られてきたbool値（true/false）をそのまま自分のプロパティに代入する
        IsStartSignalActive = isActive;
    }

    // --- 👆ここまで ---

    /// <summary>
    /// 毎フレーム、インタプリタから呼ばれて現在の全センサー状態を返す関数
    /// </summary>
    public SensorData GetSensorData()
    {
        SensorData data = new SensorData();

        // STセンサー（プロパティ経由で現在の状態を安全に取得）
        data.StM = IsStartSignalActive;

        // 対物センサー
        if (enemyLeftOrigin != null)
            data.EnemyLeft = Physics.Raycast(enemyLeftOrigin.position, enemyLeftOrigin.forward, enemySensorLength);

        if (enemyRightOrigin != null)
            data.EnemyRight = Physics.Raycast(enemyRightOrigin.position, enemyRightOrigin.forward, enemySensorLength);

        // 床センサー
        if (lineLeftOrigin != null)
            data.LineLeft = Physics.Raycast(lineLeftOrigin.position, -lineLeftOrigin.up, lineSensorLength);

        if (lineRightOrigin != null)
            data.LineRight = Physics.Raycast(lineRightOrigin.position, -lineRightOrigin.up, lineSensorLength);

        return data;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (enemyLeftOrigin != null) Gizmos.DrawRay(enemyLeftOrigin.position, enemyLeftOrigin.forward * enemySensorLength);
        if (enemyRightOrigin != null) Gizmos.DrawRay(enemyRightOrigin.position, enemyRightOrigin.forward * enemySensorLength);

        Gizmos.color = Color.white;
        if (lineLeftOrigin != null) Gizmos.DrawRay(lineLeftOrigin.position, -lineLeftOrigin.up * lineSensorLength);
        if (lineRightOrigin != null) Gizmos.DrawRay(lineRightOrigin.position, -lineRightOrigin.up * lineSensorLength);
    }
}