﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
	public enum AnimationType {
		Idle,
		Run,
		IsHit,
		Death,
        Attack
	}    	

    public enum ActionSequenceType {
        MoveToNextScene,
        HitEnemy,
        ReceiveDamage,
        Die
    }

	private enum ActionType {
		Move,
		Attack,
        ReceiveDamage,
        Die
	} 

    private enum MoveType {
        ToEnemy,
        FromEnemy,
        ToNextScene
    }
    private struct ActionInfo {
        public ActionInfo(ActionType aType) {
            actionType = aType;
            damage = 0;
            destination = new Vector3(0.0f, 0.0f, 0.0f);
            moveType = MoveType.ToEnemy;
        }

        public ActionInfo(ActionType aType, int dam, Vector3 dest, MoveType movType) {
            actionType = aType;
            damage = dam;
            destination = dest;
            moveType = movType;
        }

        public ActionType actionType;
        public int damage;
        public Vector3 destination;
        public MoveType moveType;
    };

    private List<ActionInfo> actionsQueue = new List<ActionInfo>();
    private ActionInfo currentActionInfo = new ActionInfo(); 

    public struct CharacterStats {
        public CharacterStats(int health) {
            HP = health;
            damage = 50;
            movSpeed = 0.5f;
            critChance = 0.1f;
            critMult = 2.1f;
            team = Game.teamType.Players;
        }

        public CharacterStats(int health, int dmg, float speed, float cChance, float cMult, Game.teamType t) {
            HP = health;
            damage = dmg;
            movSpeed = speed;
            critChance = cChance;
            critMult = cMult;
            team = t;
        }

        public int HP;
        public int damage;
        public float movSpeed;
        public float critChance;
        public float critMult;
        public Game.teamType team;
    }

    private struct CharacterState {
        public string name;
        public int HP;
        public int damage;
        public float damageBuff;
        public float critChance;
        public float critMult;
        public float speed; // per unit time 
        // public ActionState actionState;
        public bool inAction;
        public bool alive;
        public bool isMoving;
        public Vector3 moveDirection;
        public Vector3 viewPortPosition;
        public Vector3 lastViewPortPosition; // position before last moveAction
        public  AnimationType curAnimationType;
        public Game.teamType team;
    }

    public struct SceneInfo {
        public Animator animator;
        public GameObject gameObject;
        public Camera gameCamera;
    }

    private Dictionary<AnimationType, string> animationTypeToString = new Dictionary<AnimationType, string>();

    private CharacterState characterState;
    private SceneInfo sceneEntities;
    [SerializeField] private Game game;
    public AnimationClip Run;
    public AnimationClip Idle;
    public AnimationClip Attack;
    public AnimationClip Death;
    public AnimationClip IsHit;

    public int attackPixelOffset; // should also be based on team facing right/left ...
    private int teamID;

    public void init(CharacterStats characterStats) {
        teamID = 0;

        // REMOVE " (UnityEngine.AnimationClip)" from end of string(28 characters). EXTREMELY UNRELIABLE
        string idle = Idle.ToString();
        idle = idle.Remove(idle.Length - 28);
        string run = Run.ToString();
        run = run.Remove(run.Length - 28);
        string isHit = IsHit.ToString();
        isHit = isHit.Remove(isHit.Length - 28);
        string attack = Attack.ToString();
        attack = attack.Remove(attack.Length - 28);
        string death = Death.ToString();
        death = death.Remove(death.Length - 28);

        animationTypeToString.Add(AnimationType.Idle, idle);
        animationTypeToString.Add(AnimationType.Run, run);
        animationTypeToString.Add(AnimationType.IsHit, isHit);
        animationTypeToString.Add(AnimationType.Death, death);
        animationTypeToString.Add(AnimationType.Attack, attack);

        characterState = new CharacterState();
        characterState.name = "NoName";
        characterState.HP = characterStats.HP;
        characterState.speed = characterStats.movSpeed;
        characterState.damage = characterStats.damage;
        characterState.damageBuff = 1.0f;
        characterState.critChance = characterStats.critChance;
        characterState.critMult = characterStats.critMult;
    	characterState.curAnimationType = AnimationType.Idle;
        characterState.isMoving = false;
        characterState.alive = true;
        // characterState.actionState = ActionState.Idle;
        characterState.inAction = false;
        characterState.moveDirection = new Vector3(1.0f, 0.0f, 0.0f);
        characterState.team = characterStats.team;

        sceneEntities = new SceneInfo();
        sceneEntities.animator = this.gameObject.GetComponent<Animator>();
        sceneEntities.gameObject = this.gameObject;
        sceneEntities.gameCamera = game.getMainCamera();
        
        characterState.viewPortPosition = sceneEntities.gameCamera.WorldToViewportPoint(sceneEntities.gameObject.transform.position);
        characterState.lastViewPortPosition = characterState.viewPortPosition;

        setViewPortPosition(characterState.viewPortPosition);
        startAnimation(AnimationType.Idle);

        registerInScene();
    }

    public string getName() {
        return characterState.name;
    }

    public void setName(string Name) {
        characterState.name = Name;
    }

    public int getTeamID() {
        return teamID;
    }

    public void setTeamID(int ID) {
        teamID = ID;
    }

    private void startAnimation(AnimationType animationType) {
        // print( "STARTED ANIMATION : " + animationTypeToString[animationType]);

        sceneEntities.animator.StopPlayback();
        sceneEntities.animator.Play(animationTypeToString[animationType]);
        characterState.curAnimationType = animationType;
    }

    private void setViewPortPosition(Vector3 vPpos) {
        Vector3 worldPos = sceneEntities.gameCamera.ViewportToWorldPoint(vPpos);
        // worldPos.z = vPpos.z;
        // Debug.Log("position set : " + worldPos.ToString());
        sceneEntities.gameObject.transform.position = worldPos;
        characterState.viewPortPosition = vPpos;
    }

    public Vector3 getViewPortPosition() {
        return characterState.viewPortPosition;
    }

    public int getAttackDamage() {
        bool willCrit = characterState.critChance >= Random.Range(0.0f, 1.0f);
        if (willCrit) {
             return (int)Mathf.Round((float)characterState.damage * characterState.damageBuff * characterState.critMult * characterState.damageBuff);
        } else {
            return (int)Mathf.Round((float)characterState.damage * characterState.damageBuff);
        }
    }

    // public void setEnemy(Character enemy) {
    //     gameInfo.curEnemy = enemy;
    // }

    public bool isInAction() {
        return characterState.inAction;
    }

    private Vector3 getCorrectPositionToAttack() {
        Vector3 position = game.getCurrentEnemy().getViewPortPosition(); 
        Vector3 pixelPosition = sceneEntities.gameCamera.ViewportToScreenPoint(position);
        pixelPosition.x += attackPixelOffset * (-1.0f);
        position = sceneEntities.gameCamera.ScreenToViewportPoint(pixelPosition);

        return position;
    }

    private void startQueueAction(ActionInfo actionInfo) {
        currentActionInfo = actionInfo;
        characterState.inAction = true;

        if (currentActionInfo.actionType == ActionType.Move) {
            characterState.isMoving = true;

            if (currentActionInfo.moveType == MoveType.ToEnemy) {
                currentActionInfo.destination = getCorrectPositionToAttack();
            } else if(currentActionInfo.moveType == MoveType.ToNextScene) { 
                currentActionInfo.destination = game.getNextScenePositionForCharacterID(teamID); 
            } else if(currentActionInfo.moveType == MoveType.FromEnemy) {
                currentActionInfo.destination = characterState.lastViewPortPosition;
            } else {
                Debug.LogError("Incorrect MoveType in startQueueAction !");
            }

            // print ("DESTINATION CHANGED ! d : " + currentActionInfo.destination);

            characterState.lastViewPortPosition = characterState.viewPortPosition;
            Vector3 toDestination = currentActionInfo.destination - characterState.viewPortPosition;
            // print("TO DEST : " + toDestination.ToString());
            // toDestination.z = 0.0f;
            characterState.moveDirection = toDestination.normalized;
        } else if (currentActionInfo.actionType == ActionType.Attack) {
            startAnimation(getAssociatedAnimationType(currentActionInfo.actionType));
        } else if (currentActionInfo.actionType == ActionType.ReceiveDamage) {

            // print("ISHIT ANIM STARTED");
            characterState.HP -= currentActionInfo.damage;
            startAnimation(AnimationType.IsHit);
        } 
    }

    private void updateNextScenePosition(float xCoord) {
        // gameInfo.nextScenePosition = getGameController().getNextScenePositionForPlayer(playerId);
    }

    public Game.teamType getTeam() {
        return characterState.team;
    }

    public bool getAlive() {
        return characterState.alive;
    }

    private void addAttackAction() {
        ActionInfo attackInfo = new ActionInfo(ActionType.Attack);
        attackInfo.damage = getAttackDamage();

        actionsQueue.Add(attackInfo);
    }

    private void addMoveAction(MoveType moveType) {
        ActionInfo movementInfo = new ActionInfo(ActionType.Move);
        movementInfo.moveType = moveType;

        Debug.Log("Added move action " + moveType.ToString());
        Debug.Log("Destination : " + movementInfo.destination.ToString());
        Debug.Log("My position : " + characterState.viewPortPosition.ToString());

        actionsQueue.Add(movementInfo);
    }

    private void addReceiveDamageAction(int damage) {
        ActionInfo receiveDamageInfo = new ActionInfo(ActionType.ReceiveDamage);
        receiveDamageInfo.damage = damage;

        actionsQueue.Add(receiveDamageInfo);
    }

    private AnimationType getAssociatedAnimationType(ActionType actionType) {
        switch(actionType) {
            case ActionType.Attack :
                return AnimationType.Attack;
            case ActionType.Die :
                return AnimationType.Death;
            case ActionType.ReceiveDamage :
                return AnimationType.IsHit;
            case ActionType.Move :
                return AnimationType.Run;
            default : 
                Debug.LogError("Unsupported actionType in AnimationType");
                break;
        }

        return AnimationType.Attack;
    }

    public void receiveDamage(int damage) {
        addReceiveDamageAction(damage);
    }
    private void clearActionQueueAndDie() {
        actionsQueue.Clear();
        startAnimation(AnimationType.Death);
        characterState.alive = false;
    }

    public void dealDamage() {
        int damage = currentActionInfo.damage;

        game.getCurrentEnemy().receiveDamage(damage);

        // print("DEALT " + damage.ToString() + " DAMAGE !!!");
    }

    public void addActionSequence(ActionSequenceType actionSequenceType) {
        switch(actionSequenceType) {
            case ActionSequenceType.HitEnemy :
                addMoveAction(MoveType.ToEnemy);
                addAttackAction();
                addMoveAction(MoveType.FromEnemy);
                break;
            case ActionSequenceType.MoveToNextScene :
                addMoveAction(MoveType.ToNextScene);
                break;

            default :
                Debug.LogError("Incorrect ActionSequenceType in addActionSequence !");
                break;
        }
    }

    public void setAsEnemy() {
        // enable red circle under
    }

    public void unsetAsEnemy() {
        // disable red circle under
    }

    public void endAction() {
        Debug.Log(" END ACTION " + currentActionInfo.actionType.ToString());

    	characterState.inAction = false;
    	startAnimation(AnimationType.Idle);
        characterState.isMoving = false;

        if (actionsQueue.Count != 0) {
            actionsQueue.RemoveAt(0);
        }

        if (characterState.HP <= 0) {
            clearActionQueueAndDie();
        }
    }

    public void registerInScene() {
        game.registerCharacter(this);
    }

    private void updateCurrentAction() {
        if (characterState.inAction == false) {
            return;
        } 

        // Debug.Log(actionsQueue[0].actionType.ToString());

         // update position
        if (characterState.isMoving) {
            // Debug.Log("MOVING");
            // this.gameObject.transform.position = this.gameObject.transform.position;
            Vector3 left = currentActionInfo.destination - characterState.viewPortPosition;
            // print ("position : " + characterState.viewPortPosition.ToString());
            float distanceLeft = left.magnitude;
            float distanceToTravel = Time.deltaTime * characterState.speed;
            // print("LEFT : " + distanceLeft.ToString());
            // print("VECTOR LEFT : " + left.ToString());
            if (distanceToTravel > distanceLeft) {
                setViewPortPosition(currentActionInfo.destination);
                endAction();
            } else {
                setViewPortPosition(characterState.viewPortPosition + characterState.moveDirection * distanceToTravel);
            }

            // Debug.Log(distanceToTravel.ToString());
            // Debug.Log(characterState.moveDirection.ToString());

        }

        // update animation if still in action. TODO :: CHANGE THIS 
        AnimationType neededCurAnimationType = getAssociatedAnimationType(currentActionInfo.actionType);
        if ((characterState.curAnimationType != neededCurAnimationType) && (characterState.inAction == true)) {
            startAnimation(neededCurAnimationType);
        }
    }

    private int actionsCount = 0;
    void Update()
    {
        if (characterState.alive == false) {
            return;
        }

        updateCurrentAction();

        // start new action from queue if any. Comes last on update()
        if (characterState.inAction == false) {

            // Debug.Log("INACTION FALSE");
            if (actionsQueue.Count != 0) {
                actionsCount++;
                Debug.Log(actionsCount.ToString());
                startQueueAction(actionsQueue[0]);
            }
        }
    }
}
