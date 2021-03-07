using UnityEngine;


public class ShooterMsgType {

	// Client to server
	public static short MSG_LOGIN = 51;
	public static short MSG_START = 52;
	public static short MSG_FINISH = 55;

	// Server to client
	public static short MSG_LOGIN_RESPONSE = 1000;
	public static short MSG_SET_STATE = 1001;
	public static short MSG_SCORE = 1005;
	public static short MSG_PING = 1006;

};



// ---------- Client to Server Messages -------------------

public struct LoginMessage
{
	public string playerName;
}

public struct StartMessage
{
}

public struct FinishMessage
{
}

// ---------- Server tp Client Messages -------------------

public struct LoginResponseMessage
{
	public int playerId;
}

public struct SetStateMessage
{
	public int state;
}

public struct ScoreMessage
{
	public int score;
	public Vector3 scorePos;
	public int lives;
}

public struct PingMessage
{
	public int seqNr;
	public float time;
}
