using System.Collections;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    int _minValue = 1;
    int _maxValue = 20;
    int _initialNumberCount = 1;
    int _erasersCount = 1;
    float _eraserSpawnRate = 10f;
    float _numberSpawnRate = 0.75f;
    int _goal = 50;
    bool _fixedGoal = true;
    bool _canSpawnNumbers = true;
    readonly Vector2 _numberRadius = new Vector2(2f, 2f);
    Player _player;
    GameObject _parentObject;
    [SerializeField] GameObject storagePrefab;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject numberPrefab;
    [SerializeField] GameObject eraserPrefab;
    [SerializeField] LevelSizeHolder levelSizeHolder;
    [SerializeField] LayerMask spawnCollisionLayer;
    [SerializeField] Commentator commentator;
    [SerializeField] BoundsSetup bounds;

    public System.Action SwitchLevel;
    public System.Action Death;
    public System.Action CloseUI;

    Coroutine _spawnErasersCoro;
    Coroutine _spawnNumbersCoro;
    Vector2 _levelSize;

    public void OnPlayerDeath()
    {
        Debug.Log(">пук");
        if (_spawnErasersCoro != null)
        {
            StopCoroutine(_spawnErasersCoro);
        }
        if (_spawnNumbersCoro != null)
        {
            StopCoroutine(_spawnNumbersCoro);
        }
        // Death();

        const float wait = 1.5f;
        Invoke(nameof(DeathLogic), wait);
    }

    void DeathLogic() {
        Death();
        FinishLevel();
        commentator.AnnounceDeath();
    }

    void OnStore(int value)
    {
        if (value >= _goal)
        {
            if (_fixedGoal && value > _goal)
                return;
            Debug.Log($"Goal achieved! {value}");
            commentator.CloseAll();
            FinishLevel();
            SwitchLevel();
        }
    }

    void FinishLevel()
    {
        bounds.SetVisible(false);
        Destroy(_parentObject);
    }

    public void InitializeLevel(GameSettings gameSettings, int level)
    {
        _minValue = gameSettings.MinValue;
        _maxValue = gameSettings.MaxValue;
        _erasersCount = gameSettings.ErasersCount;
        _eraserSpawnRate = gameSettings.EraserSpawnRate;
        _numberSpawnRate = gameSettings.NumberSpawnRate;
        _goal = gameSettings.Goal;
        _fixedGoal = gameSettings.FixedGoal;
        _initialNumberCount = gameSettings.InitialNumberCount;
        _canSpawnNumbers = gameSettings.CanSpawnNumbers;
        _levelSize = gameSettings.levelSize;
        levelSizeHolder.SetSize(_levelSize);

        _parentObject = new GameObject();

        commentator.SetLevel(level);
        
        SpawnPlayer();
        SpawnStorage();

        _player.SummReplic = commentator.Summ;

        for (int i = 0; i < _initialNumberCount; ++i)
        {
            var number = Instantiate(numberPrefab, GetRandomPosition(), Quaternion.identity).GetComponent<Number>();
            number.Initiate(Random.Range(_minValue, _maxValue));
            number.mapObject = _parentObject.transform;
            number.transform.SetParent(_parentObject.transform);
        }

        if (_canSpawnNumbers)
            _spawnNumbersCoro = StartCoroutine(nameof(SpawnNewNumber));
        _spawnErasersCoro = StartCoroutine(nameof(SpawnEraser));

        bounds.SetVisible(true);
    }

    void SpawnPlayer()
    {
        _player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity).GetComponent<Player>();
        _player.onDeath = OnPlayerDeath;
        _player.mapObject = _parentObject.transform;

        _player.transform.SetParent(_parentObject.transform);
    }
    void SpawnStorage()
    {
        var storage = Instantiate(storagePrefab, _parentObject.transform);
        var storageHeight = storage.GetComponent<SpriteRenderer>().size.y;
        var storageLocation = new Vector2(0, _levelSize.y / 2 - storageHeight / 2);
        storage.transform.position = storageLocation;
        storage.GetComponent<Storage>().OnStore += OnStore;

        // storage.transform.SetParent(parentObject.transform);
    }

    IEnumerator SpawnNewNumber()
    {
        for (; ; )
        {
            Vector3 spawnPoint = GetRandomPosition();
            if (spawnPoint == Vector3.zero)
                yield return null;
            var number = Instantiate(numberPrefab, spawnPoint, Quaternion.identity).GetComponent<Number>();
            number.Initiate(Random.Range(_minValue, _maxValue));
            number.mapObject = _parentObject.transform;
            number.transform.SetParent(_parentObject.transform);
            number.bounds = bounds;
            yield return new WaitForSeconds(_numberSpawnRate);
        }
    }

    IEnumerator SpawnEraser()
    {
        for (var i = 0; i < _erasersCount; ++i)
        {
            yield return new WaitForSeconds(_eraserSpawnRate);
            GameObject eraser = Instantiate(eraserPrefab, GetRandomPosition(), Quaternion.identity);
            commentator.EnemyAppear(i);

            eraser.transform.SetParent(_parentObject.transform);
        }
    }

    Vector2 GetRandomPosition()
    {
        RaycastHit2D hit;
        var position = Vector2.zero;

        var counter = 1000;
        do
        {
            if (counter-- == 0)
            {
                Debug.Log("FUCK YOU");
                break;
            }

            const float BOUNDDELTA = 0.5f;
            
            var x = Random.Range(-_levelSize.x / 2 + BOUNDDELTA, _levelSize.x / 2 - BOUNDDELTA);
            var y = Random.Range(-_levelSize.y / 2 + BOUNDDELTA, _levelSize.y / 2 - BOUNDDELTA);
            position = new Vector3(x, y);
            hit = Physics2D.BoxCast(position, _numberRadius, 0, Vector2.up, 0, spawnCollisionLayer);

        } while (hit);

        return position;
    }
}
