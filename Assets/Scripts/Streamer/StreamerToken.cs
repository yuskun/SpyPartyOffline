using Fusion;

/// <summary>
/// Streamer 客端用 ConnectionToken 帶這個簽名上線，
/// host 端用 IsStreamer(runner, player) 比對來決定要不要跳過 spawn。
/// 不是強加密；只是一個固定簽名。
/// </summary>
public static class StreamerToken
{
    private static readonly byte[] Signature = System.Text.Encoding.UTF8.GetBytes("STREAMER-V1");

    /// <summary>Streamer 加房時塞進 StartGameArgs.ConnectionToken</summary>
    public static byte[] Get()
    {
        // 回傳 copy 避免被外部 mutate
        var copy = new byte[Signature.Length];
        System.Buffer.BlockCopy(Signature, 0, copy, 0, Signature.Length);
        return copy;
    }

    /// <summary>Host 端判斷某個 player 是不是 streamer</summary>
    public static bool IsStreamer(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null) return false;
        byte[] bytes;
        try { bytes = runner.GetPlayerConnectionToken(player); }
        catch { return false; }
        if (bytes == null || bytes.Length != Signature.Length) return false;
        for (int i = 0; i < Signature.Length; i++)
            if (bytes[i] != Signature[i]) return false;
        return true;
    }
}
