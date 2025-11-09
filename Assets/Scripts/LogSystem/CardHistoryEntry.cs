using System;
using UnityEngine;

/// <summary>
/// �����@���d���ϥξ��{����Ƶ��c
/// �Ω�l�ֹܽ�֨ϥΤF����d�B��ɨϥΡB���G�p��
/// </summary>
[Serializable]
public class CardHistoryEntry
{
    /// <summary>
    /// �ϥΥd�������a ID
    /// �]�Ψӿ��ѬO���쪱�a�o�ʤF�ޯ�ήĪG�^
    /// </summary>
    public int userId;

    /// <summary>
    /// �Q�ϥΡ]�Χ����^�ؼЪ����a ID  
    /// �]�Y�d���L�ؼСA�h�i�]�� -1 �άۦP���a ID�^
    /// </summary>
    public int targetId;

    /// <summary>
    /// �Q�ϥΪ��d���W��  
    /// �]�Ҧp�G�u�������ȥd�v�B�u�v���D��d�v�B�u�����\��d�v���^
    /// </summary>
    public string cardName;

    /// <summary>
    /// �d��������  
    /// �i��ȡG
    /// - <see cref="CardType.Mission"/>�G���ȥd�A�q�`�P�ؼЩα�����  
    /// - <see cref="CardType.Function"/>�G�\��d�A�q�`�O�Y�ɮĪG�]�p�����B�洫�^  
    /// - <see cref="CardType.Item"/>�G���~�d�A���ѹD��ίS���W�q  
    /// - <see cref="CardType.None"/>�G�L�ĩμȵL����
    /// </summary>
    public CardType cardType;

    /// <summary>
    /// ���ȥd��������������]Trigger / Collect ���^  
    /// �D���ȥd�ɥi�� null  
    /// - Trigger�GĲ�o���]�Ҧp�Y�ƥ�F���ɱҰʡ^  
    /// - Collect�G�������]�ݭn�����S�w����α���^
    /// </summary>
    public MissionType? missionType; // �i�� null

    /// <summary>
    /// CanUse() ���ˬd���G  
    /// - true�G���\�ϥ�  
    /// - false�G�ϥΥ��ѡ]���󤣲šB�N�o���B�ؼп��~���^
    /// </summary>
    public bool canUseResult;

    /// <summary>
    /// �����d���ާ@���ɶ��W�O  
    /// �]�O���ʧ@�o�ͪ���ڮɶ��A�i�Ω�Ƨǩβέp�^
    /// </summary>
    public DateTime timeStamp;

    /// <summary>
    /// �غc�l�A�Ω��l�Ƥ@�����㪺���{���
    /// </summary>
    public CardHistoryEntry(int userId, int targetId, string cardName, CardType cardType, MissionType? missionType) //, bool canUseResult
    {
        this.userId = userId;
        this.targetId = targetId;
        this.cardName = cardName;
        this.cardType = cardType;
        this.missionType = missionType;
        //this.canUseResult = canUseResult;
        this.timeStamp = DateTime.Now; // �۰ʰO�����U�ɶ�
    }
}
