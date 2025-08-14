using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

// RaiseEvent 이벤트 코드(0~199사이 정의 가능)
public enum EventCode : byte
{
    enable,
    setParent,
}

public class RaiseEventReceiver : MonoBehaviour, IOnEventCallback
{
    // 포톤네트워크 콜백 이벤트에 이것을 추가!
    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }
    // 포톤네트워크 콜백 해제
    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    // 이벤트 호출했을 때 여기로
    public void OnEvent(EventData photonEvent)
    {
        EventCode eventCode = (EventCode)photonEvent.Code;
        switch (eventCode)
        {
            // 이벤트 enable : PhotonView 컴포넌트를 포함한 오브젝트를 ViewID를 통해 찾아 모든 클라이언트에서 활성화
            case EventCode.enable:
                Event_enable(photonEvent.CustomData);
                break;
            case EventCode.setParent:
                int[] datas = (int[])photonEvent.CustomData;
                Event_setParent(datas);
                break;
            default:
                break;
        }
    }

    void Event_enable(object eventData)
    {
        object[] data = (object[])eventData;
        int viewID = (int)data[0];
        Vector3 pos = (Vector3)data[1];
        Quaternion rot = (Quaternion)data[2];

        PhotonView objectToEnable = PhotonView.Find(viewID);
        if (objectToEnable != null)
        {
            var go = objectToEnable.gameObject;
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
        }
    }

    void Event_setParent(int[] datas)
    {
        // 부모
        PhotonView parent = PhotonView.Find(datas[0]);
        // 자식
        PhotonView sibling = PhotonView.Find(datas[1]);
        if (parent != null && sibling != null)
            sibling.transform.SetParent(parent.transform);
        //else
            //Debug.Log("둘 중 하나를 찾을 수 없어 부모, 자식 관계를 형성할 수 없습니다.");
    }
}
