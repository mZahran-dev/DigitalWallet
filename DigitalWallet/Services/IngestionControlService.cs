namespace DigitalWallet.Services;

public class IngestionControlService
{
    private volatile bool _isIngestionActive = true;

    public bool IsIngestionActive => _isIngestionActive;

    public void Pause() => _isIngestionActive = false;

    public void Resume() => _isIngestionActive = true;
}
