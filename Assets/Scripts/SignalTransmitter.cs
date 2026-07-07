using UnityEngine;

public class SignalTransmitter : MonoBehaviour
{
    /// <summary>
    /// 信号の状態が変更されたことを一斉に通知するための拡声器（イベント）
    /// true: スタート信号ON / false: エンド（ストップ）信号
    /// </summary>
    public static System.Action<bool> OnSignalChanged;

    /// <summary>
    /// 【UIボタン用】StartSignalボタンが押されたときに実行する関数
    /// </summary>
    public void SendStartSignal()
    {
        Debug.Log("[SignalTransmitter] スタート信号を一斉送信しました。");
        // 登録されているすべてのロボット（リスナー）へ「true」を一斉に叫ぶ
        OnSignalChanged?.Invoke(true);
    }

    /// <summary>
    /// 【UIボタン用】EndSignalボタンが押されたときに実行する関数
    /// </summary>
    public void SendEndSignal()
    {
        Debug.Log("[SignalTransmitter] エンド信号を一斉送信しました。");
        // 登録されているすべてのロボット（リスナー）へ「false」を一斉に叫ぶ
        OnSignalChanged?.Invoke(false);
    }
}