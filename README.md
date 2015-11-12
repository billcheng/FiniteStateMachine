# FiniteStateMachine
C# Fluent Finite State Machine for Unity3D

<h2>Installation</h2>
Copy StateMachine.cs to your Assets folder

<h2>Example</h2>

          StateMachine sm = new StateMachine("main");
          sm
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
                }, "decide");
