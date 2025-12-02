using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class LobbyBird : MonoBehaviour
{
    [SerializeField] GameObject bird;
    [SerializeField] Transform startPoint;
    [SerializeField] Transform endPoint;
    [SerializeField] float moveTime;
    [SerializeField] float startSound;
    private WaitForSeconds startDelay;
    private WaitForSeconds wStartSound;
    private bool canBirdsong = true;

    private LobbySkyController skyController;
    private Material afternoonSkybox;
    private Material nightSkybox;
    private Material dawnSkybox;

    private void Awake()
    {
        startDelay = new WaitForSeconds(Random.Range(17f, 23f));
        skyController = FindFirstObjectByType<LobbySkyController>();
        afternoonSkybox = skyController.afternoonSkybox;
        nightSkybox = skyController.nightSkybox;
        dawnSkybox = skyController.dawnSkybox;
    }

    private void OnEnable()
    {
        skyController.OnSkyboxChanged += HandleChangedSkybox;
    }

    private void Start()
    {
        wStartSound = new WaitForSeconds(startSound);
        bird = Object.Instantiate(bird, startPoint.position, Quaternion.identity, gameObject.transform);
        bird.SetActive(false);
        StartCoroutine(MoveBird());
    }

    private void OnDisable()
    {
        skyController.OnSkyboxChanged -= HandleChangedSkybox;
        StopAllCoroutines();
    }

    private void HandleChangedSkybox(Material mat)
    {
        canBirdsong = mat != nightSkybox && mat != dawnSkybox && mat != afternoonSkybox;
    }

    private IEnumerator MoveBird()
    {
        while (true)
        {
            yield return startDelay;

            bird.transform.position = startPoint.position;
            bird.transform.LookAt(endPoint);
            bird.SetActive(true);

            if (canBirdsong) StartCoroutine(BirdSong());
            
            float t = 0;

            while (t < 1f)
            {
                t += Time.deltaTime / moveTime;
                bird.transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t);
                yield return null;
            }

            bird.SetActive(false);
        }
    }

    private IEnumerator BirdSong()
    {
        yield return wStartSound;
        int i = 0;
        while (i < 6 && bird.activeSelf)
        {
            float rand = Random.Range(0.3f, 1f);
            AudioEvents.Raise(AmbientKey.Birdsong, index: -1, pooled: true, pos: bird.transform.position);
            yield return new WaitForSeconds(rand);
            i++;
        }
    }
}
