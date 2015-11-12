using UnityEngine;
using System.Collections;

public class ChopperAI : MonoBehaviour
{

    StateMachine sm = new StateMachine("main");
    StateMachine subsm = new StateMachine("helicopter");
    public float Height = 50;
    public float AscendingSpeed = 1;
    public float DescendingSpeed = 1;
    //public float MomentumSpeed = 1;
    public float MoveSpeed = 1;
    public float TurningSpeed = 1;
    public Vector3[] PatrolCheckPoints;
    private Vector3 _moveTo;
    int _patrolIndex;
    public bool Verbose = false;

    // Use this for initialization
    void Start()
    {
        SetupMain();
        SetupSub();
        sm.Verbose = subsm.Verbose = Verbose;
    }

    private void SetupMain()
    {
        float time = 0;

        sm
            .State("idle")
                .Reset()
                .OnEntry(() => time = Time.time)
                .Condition(() => Time.time - time >= 3, "patrol")
            .State("patrol")
                .OnEntry(() =>
                {
                    // find the nearest patrol check point
                    subsm.Reset("decide");
                    _patrolIndex = PatrolCheckPoints.FindMinIndex(p => Vector3.Distance(p, transform.position));
                    _moveTo = _patrolIndex != -1 ? PatrolCheckPoints[_patrolIndex] : transform.position;
                })
                .OnState(() => subsm.Update())
            .State("attack")
            .State("retreat")
            .State("heal")
            ;
    }

    private void SetupSub()
    {
        var originalPos = Vector3.zero;
        float speed = 0;
        float percent = 0;
        Vector3 tempTarget = Vector3.zero;

        subsm
            .State("decide")
                .Condition(() => transform.position.y - _moveTo.y > 50, "descending")
                .Condition(() => transform.position.y - _moveTo.y < -50, "ascending")
                .Condition(() => true, "alignAngle")
            .State("ascending")
                .OnEntry(() =>
                {
                    originalPos = transform.position;
                    tempTarget = new Vector3(transform.position.x, _moveTo.y, transform.position.z);
                    percent = 0;
                })
                .OnState(() =>
                {
                    speed = percent.HillSlope(1, 0.5f, 4);
                    percent = Mathf.Min(percent + AscendingSpeed * Time.deltaTime, 2f);

                    transform.position = Vector3.Lerp(originalPos, tempTarget, speed);
                })
                .Condition(() => Vector3.Distance(transform.position, tempTarget) <= 5, "moveTo")
            .State("descending")
                .Reset()
                .OnEntry(() =>
                {
                    originalPos = transform.position;
                    percent = 0;

                    RaycastHit hitInfo;
                    if (Physics.Raycast(transform.position, Vector3.down, out hitInfo))
                        tempTarget = new Vector3(transform.position.x, hitInfo.point.y, transform.position.z);
                })
                .OnState(() =>
                {
                    speed = percent.HillSlope(1, 0.5f, 4);
                    percent = Mathf.Min(percent + DescendingSpeed * Time.deltaTime, 2f);
                    transform.position = Vector3.Lerp(originalPos, tempTarget, speed);
                })
                .Condition(() => Vector3.Distance(transform.position, tempTarget) <= 1, "moveTo")
            .State("alignAngle")
                .OnEntry(() =>
                {
                })
                .OnState(() =>
                {
                    var dir = _moveTo - transform.position;
                    dir.y = 0;
                    var newRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, newRot, TurningSpeed * Time.deltaTime);

                    transform.Translate(Vector3.forward * MoveSpeed * Time.deltaTime);

                    if (transform.position.y < _moveTo.y-0.5)
                        transform.Translate(Vector3.up * MoveSpeed * Time.deltaTime);
                    if (transform.position.y > _moveTo.y + 0.5)
                        transform.Translate(Vector3.down * MoveSpeed * Time.deltaTime);

                })
                .Condition(() =>
                {
                    var dir = _moveTo - transform.position;
                    dir.y = 0;
                    var angle = Vector3.Angle(transform.forward, dir);
                    if (angle < 15)
                        return true;

                    if (Vector3.Distance(_moveTo, transform.position) <= 1)
                        return true;

                    return false;
                }, "moveTo")
            .State("moveTo")
                .OnEntry(() =>
                {
                    originalPos = transform.position;
                    percent = 0;
                })
                .OnState(() =>
                {
                    var dir = (_moveTo - transform.position).normalized;

                    dir.y = 0;
                    var newRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, newRot, TurningSpeed * Time.deltaTime);

                    transform.Translate(Vector3.forward * MoveSpeed * Time.deltaTime);

                    if (transform.position.y < _moveTo.y - 0.5)
                        transform.Translate(Vector3.up * MoveSpeed * Time.deltaTime);
                    if (transform.position.y > _moveTo.y + 0.5)
                        transform.Translate(Vector3.down * MoveSpeed * Time.deltaTime);
                })
                .Condition(() =>
                {
                    var result = Vector3.Distance(transform.position, _moveTo) <= 1;
                    if (result)
                    {
                        _patrolIndex = (_patrolIndex + 1) % PatrolCheckPoints.Length;
                        _moveTo = PatrolCheckPoints[_patrolIndex];
                    }
                    return result;
                }, "decide")

        ;
    }

    //private float _forwardAngle = 0;

    private Vector3 lastPos=Vector3.zero;
    // Update is called once per frame
    void Update()
    {
        sm.Update();

        if (lastPos != Vector3.zero)
        {
            var distance = Vector3.Distance(lastPos, transform.position);
            //Debug.Log(distance);

            var _forwardAngle = Mathf.Min(distance*300, 30);

            var newRot = Quaternion.AngleAxis(_forwardAngle, Vector3.right);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, newRot, 2);
        }
        lastPos = transform.position;
        //if (subsm.Current.Name == "moveTo")
        //{
        //    Debug.Log("moveTO " + _forwardAngle);
        //    if (_forwardAngle <= 20)
        //    {
        //        _forwardAngle = Mathf.Min(_forwardAngle + 10 * Time.deltaTime, 20);

        //        var newRot = Quaternion.AngleAxis(_forwardAngle, Vector3.right);
        //        transform.rotation = Quaternion.RotateTowards(transform.rotation, newRot, 2);
        //    }
        //}
        //else
        //{
        //    if (_forwardAngle > 0)
        //    {
        //        _forwardAngle = Mathf.Max(_forwardAngle - 10*Time.deltaTime, 0);

        //        var newRot = Quaternion.AngleAxis(_forwardAngle, Vector3.right);
        //        transform.rotation = Quaternion.RotateTowards(transform.rotation, newRot, 2);
        //    }
        //}

    }

    void OnDrawGizmos()
    {
        if (_moveTo != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_moveTo, 1);
        }
    }

}
