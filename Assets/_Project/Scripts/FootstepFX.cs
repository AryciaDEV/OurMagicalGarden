using Photon.Pun;
using UnityEngine;

public class FootstepFX : MonoBehaviourPun
{
    [Header("Audio")]
    public AudioClip[] stepClips;
    [Range(0f, 1f)]
    public float footstepVolume = 0.3f; // Ayak sesi için özel volume (0-1)
    public float stepIntervalWalk = 0.45f;
    public float stepIntervalRun = 0.30f;

    [Header("VFX")]
    public ParticleSystem stepVfxPrefab;
    public Transform vfxSpawnPoint;

    [Header("Move detect")]
    public float minMoveSpeed = 0.25f;

    private CharacterController cc;
    private float timer;
    private float lastPitch = 1f;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (!photonView.IsMine)
            enabled = false;
    }

    private void Update()
    {
        if (cc == null) return;
        if (!cc.isGrounded) return;

        Vector3 v = cc.velocity;
        v.y = 0f;

        float speed = v.magnitude;
        if (speed < minMoveSpeed) { timer = 0f; return; }

        float interval = (speed > 5.2f) ? stepIntervalRun : stepIntervalWalk;

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            PlayStep();
            timer = 0f;
        }
    }

    private void PlayStep()
    {
        if (stepClips == null || stepClips.Length == 0) return;

        var clip = stepClips[Random.Range(0, stepClips.Length)];

        // Pitch'i rastgele ayarla (sonraki çalmada kullanýlacak)
        lastPitch = Random.Range(0.92f, 1.08f);

        // SoundFXManager üzerinden çal - global volume otomatik uygulanýr
        if (SoundFXManager.Instance != null)
        {
            // Pitch desteði için PlaySoundWithPitch kullan (aþaðýda eklenecek)
            SoundFXManager.Instance.PlaySoundWithPitch(clip, footstepVolume, lastPitch);
        }
        else
        {
            // Fallback: Eski yöntem (SoundFXManager yoksa)
            Debug.LogWarning("[FootstepFX] SoundFXManager bulunamadý!");
        }

        // VFX
        if (stepVfxPrefab)
        {
            Vector3 pos = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position + Vector3.up * 0.05f;
            var vfx = Instantiate(stepVfxPrefab, pos, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, 2f);
        }
    }
}