using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StateMachine
{
    private Dictionary<string, StateModel> States { get; set; }
    public StateModel Current { get; private set; }
    private StateModel _old;
    public string Name { get; private set; }
    public bool Verbose = false;

    public StateMachine(string name)
    {
        States = new Dictionary<string, StateModel>();
        _old = Current = null;
        Name = name;
    }

    public void Reset(StateModel stateModel)
    {
        _old = null;
        Current = stateModel;
    }

    public void Reset(string stateName)
    {
        if (States.ContainsKey(stateName))
            Current = States[stateName];
        else
            throw new Exception("StateModel name not found");
    }

    public StateModel State(string stateName)
    {
        var result = new StateModel(this)
        {
            Name = stateName
        };

        States.Add(stateName, result);

        return result;
    }

    public void Update()
    {
        if (Current == null)
            return;

        if (Current.Id != (_old != null ? _old.Id : Guid.Empty))
        {
#if DEBUG
//            if (Verbose)
//            {
//                if (_old == null)
//                    Debug.Log(Time.time + "s " + Name + ": Initial StateModel=" + Current.Name);
//                else
//                    Debug.Log(Time.time + "s " + Name + ": " + _old.Name + " -> " + Current.Name);
//            }
#endif

            Current.ExecEntry();
            _old = Current;
        }

        Current.ExecState();

        StateModel next;
        var trigger = Current.Update(out next);

        if (trigger)
        {
            Current.ExecExit();
            if (Current == next)
                Current.ExecEntry();
            else
                Current = next;
        }
    }

    private StateModel GetState(string nextState)
    {
#if DEBUG
        if (States.ContainsKey(nextState))
            return States[nextState];

        throw new Exception(Name + " does not have state \"" + nextState + "\"");
#else
        return States[nextState];
#endif
    }

    public class StateModel
    {
        public Guid Id { get; private set; }
        public StateMachine Parent { get; private set; }
        public string Name { get; set; }
        private List<Func<bool>> Conditions { get; set; }
        private List<string> NextStates { get; set; }
        private List<Action> OnEntryEvents { get; set; }
        private List<Action> OnStateEvents { get; set; }
        private List<Action> OnExitEvents { get; set; }

        public StateModel(StateMachine parent)
        {
            Id = Guid.NewGuid();
            Parent = parent;
            Conditions = new List<Func<bool>>();
            NextStates = new List<string>();

            OnEntryEvents = new List<Action>();
            OnStateEvents = new List<Action>();
            OnExitEvents = new List<Action>();
        }

        public StateModel Condition(Func<bool> condition, string nextState)
        {
            Conditions.Add(condition);
            NextStates.Add(nextState);

            return this;
        }

        public bool Update(out StateModel state)
        {
            for (var index = 0; index < Conditions.Count; index++)
            {
                if (Conditions[index]())
                {
                    state = Parent.GetState(NextStates[index]);
                    return true;
                }
            }

            state = this;
            return false;
        }

        public StateModel State(string stateName)
        {
            return Parent.State(stateName);
        }

        public StateModel OnEntry(Action onEntry)
        {
            OnEntryEvents.Add(onEntry);
            return this;
        }

        public StateModel OnState(Action onState)
        {
            OnStateEvents.Add(onState);
            return this;
        }

        public StateModel OnExit(Action onExit)
        {
            OnExitEvents.Add(onExit);
            return this;
        }

        public StateModel Reset()
        {
            Parent.Reset(this);
            return this;
        }

        public void ExecEntry()
        {
#if DEBUG
            if (Parent.Verbose && OnEntryEvents.Count > 0)
                Debug.Log(Time.time + "s " + Parent.Name + ": " + Name + ":OnEntry");
#endif

            Exec(OnEntryEvents);
        }

        public void ExecState()
        {
            Exec(OnStateEvents);
        }

        public void ExecExit()
        {
#if DEBUG
            if (Parent.Verbose && OnExitEvents.Count > 0)
                Debug.Log(Time.time + "s " + Parent.Name + ": " + Name + ":OnExit");
#endif

            Exec(OnExitEvents);
        }

        private void Exec(List<Action> actions)
        {
            actions.ForEach(action => action.Invoke());
        }

    }

}