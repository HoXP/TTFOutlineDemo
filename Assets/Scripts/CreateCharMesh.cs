using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

public class CreateCharMesh : MonoBehaviour
{
    internal static CreateCharMesh Instance { get; private set; }

    private string xmlPath = string.Empty;
    private Dictionary<string, TTFCharInfo> _ttfCharInfoDict = null;
    [SerializeField]
    private string MeshTpl = "B";
    [SerializeField]
    private int Smooth = 5;
    private List<TTFCharInfoContourPT> controlPoints = null;
    internal List<Vector2> BezierPointList { get; private set; }
    private Vector2 invalidPoint = new Vector2(-1,0); //非法点

    //UI
    private Button btnBezier = null;
    private Image imgPointTpl = null;
    private RectTransform tranControlPoints = null;

    private void Awake()
    {
        Instance = this;

        btnBezier = transform.Find("btnBezier").GetComponent<Button>();
        btnBezier.onClick.AddListener(OnClickBtnBezier);
        imgPointTpl = transform.Find("imgPointTpl").GetComponent<Image>();
        imgPointTpl.gameObject.SetActive(false);
        tranControlPoints = transform.Find("ControlPoints").GetComponent<RectTransform>();

        xmlPath = Application.dataPath + "/DataRes/fzzy_letters.xml";

        _ttfCharInfoDict = new Dictionary<string, TTFCharInfo>();
        List<TTFCharInfo> ttfCharInfoList = GetTTFCharInfoListByXMLPath();
        for (int i = 0; i < ttfCharInfoList.Count; i++)
        {
            _ttfCharInfoDict.Add(ttfCharInfoList[i].Name, ttfCharInfoList[i]);
        }
    }
    
    private void Start()
    {
        UpdateBezierPoints();
    }
    
    void Update()
    {
        //for (int i = 0; i < controlPoints.Count - 1; i++)
        //{
        //    Vector3 pos = controlPoints[i];
        //    Vector3 posNext = controlPoints[i+1];
        //    Debug.DrawLine(pos, posNext, posNext.z % 2 == 0 ?  Color.green : Color.cyan);
        //}
        //Bezier点
        for (int i = 0; i < BezierPointList.Count - 1; i++)
        {
            if (BezierPointList[i] != invalidPoint && BezierPointList[i + 1] != invalidPoint)
            {
                Debug.DrawLine(BezierPointList[i], BezierPointList[i + 1], i % 2 == 0 ? Color.red : Color.blue);
            }
        }
    }

    private void OnClickBtnBezier()
    {
        UpdateBezierPoints();
    }

    private List<TTFCharInfo> GetTTFCharInfoListByXMLPath()
    {
        List<TTFCharInfo> tmpTTFCharInfoList = new List<TTFCharInfo>();
        XmlDocument xml = new XmlDocument();
        xml.Load(xmlPath);
        XmlNode root = xml.SelectSingleNode("/root");
        XmlNodeList nodelist = root.SelectNodes("TTGlyph");
        for (int i = 0; i < nodelist.Count; i++)
        {
            XmlNode tmpXmlNode = nodelist.Item(i);
            int xMin, yMin, xMax, yMax;
            int.TryParse(tmpXmlNode.Attributes["xMin"].Value,out xMin);
            int.TryParse(tmpXmlNode.Attributes["yMin"].Value,out yMin);
            int.TryParse(tmpXmlNode.Attributes["xMax"].Value,out xMax);
            int.TryParse(tmpXmlNode.Attributes["yMax"].Value,out yMax);
            XmlNodeList tmpXmlNodeList = tmpXmlNode.SelectNodes("contour");
            List<TTFCharInfoContour> tmpTTFCharInfoContourList = new List<TTFCharInfoContour>();
            for (int j = 0; j < tmpXmlNodeList.Count; j++)
            {
                XmlNode tmpXmlNodeContour = tmpXmlNodeList.Item(j);
                XmlNodeList tmpXmlNodeListPT = tmpXmlNodeContour.SelectNodes("pt");
                List<TTFCharInfoContourPT> tmpTTFCharInfoContourPTList = new List<TTFCharInfoContourPT>();
                for (int k = 0; k < tmpXmlNodeListPT.Count; k++)
                {
                    XmlNode tmpXmlNodePT = tmpXmlNodeListPT.Item(k);
                    int x, y, on;
                    int.TryParse(tmpXmlNodePT.Attributes["x"].Value, out x);
                    int.TryParse(tmpXmlNodePT.Attributes["y"].Value, out y);
                    int.TryParse(tmpXmlNodePT.Attributes["on"].Value, out on);
                    //Debug.Log(string.Format("{0} : {1} - {2} - {3} ; {4} - {5} - {6}", tmpXmlNode.Attributes["name"].Value, i, j, k, x, y, on));
                    TTFCharInfoContourPT tmpTTFCharInfoContourPT = TTFCharInfoContourPT.Create(x,y,on,j);
                    tmpTTFCharInfoContourPTList.Add(tmpTTFCharInfoContourPT);
                }
                TTFCharInfoContour tmpTTFCharInfoContour = TTFCharInfoContour.Create(tmpTTFCharInfoContourPTList);
                tmpTTFCharInfoContourList.Add(tmpTTFCharInfoContour);
            }
            TTFCharInfo tmpTTFCharInfo = TTFCharInfo.Create(tmpXmlNode.Attributes["name"].Value, xMin, yMin, xMax, yMax, tmpTTFCharInfoContourList);
            tmpTTFCharInfoList.Add(tmpTTFCharInfo);
        }
        return tmpTTFCharInfoList;
    }

    /// <summary>
    /// 获取二次贝塞尔点
    /// </summary>
    /// <param name="pointA">控制点，在曲线上</param>
    /// <param name="pointB">控制点，不在曲线上</param>
    /// <param name="pointC">控制点，在曲线上</param>
    /// <param name="t">t∈[0,1]</param>
    /// <returns>曲线上的Bezier点</returns>
    private Vector2 GetQuadraticBezierPoint(Vector2 pointA, Vector2 pointB, Vector2 pointC, float t)
    {
        Vector2 P = (1 - t) * (1 - t) * pointA + 2 * (1 - t) * t * pointB + t * t * pointC;
        return P;
    }

    private void UpdateBezierPoints()
    {
        string key = MeshTpl[0].ToString();
        if (!_ttfCharInfoDict.ContainsKey(key))
        {
            return;
        }

        //补齐控制点，最后一个控制点on=0的情况下，需要将该曲线的第一个点补充到后面做on=1的控制点
        controlPoints = new List<TTFCharInfoContourPT>();
        TTFCharInfo tmpTTFCharInfo = _ttfCharInfoDict[key];
        for (int i = 0; i < tmpTTFCharInfo.ContourList.Count; i++)
        {
            TTFCharInfoContour tmpTTFCharInfoContour = tmpTTFCharInfo.ContourList[i];
            for (int j = 0; j < tmpTTFCharInfoContour.PTList.Count; j++)
            {
                TTFCharInfoContourPT tmpTTFCharInfoContourPT = tmpTTFCharInfoContour.PTList[j];
                controlPoints.Add(tmpTTFCharInfoContourPT);
                if (j == tmpTTFCharInfoContour.PTList.Count - 1) // && !tmpTTFCharInfoContourPT.IsOn
                {//如果是最后一个，且on=0
                    controlPoints.Add(tmpTTFCharInfoContour.PTList[0]);
                }
            }
        }
        //控制点坐标本地化
        //for (int i = 0; i < controlPoints.Count; i++)
        //{
        //    controlPoints[i].Pos = transform.InverseTransformPoint(controlPoints[i].Pos);
        //}
        //计算Bezier点
        BezierPointList = new List<Vector2>();
        for (int i = 0; i < controlPoints.Count; i++)
        {
            TTFCharInfoContourPT tmpCur = controlPoints[i];
            if (tmpCur.IsOn)
            {
                //Debug.Log(string.Format("{0},{1}", tmpTTFCharInfoContourPT.Pos, tmpTTFCharInfoContourPT.IsOn));
                BezierPointListAdd(tmpCur.Pos);
                //获取下一个
                TTFCharInfoContourPT tmpNext = null;
                if (i + 1 < controlPoints.Count)
                {
                    tmpNext = controlPoints[i + 1];
                }
                if (tmpNext == null)
                {//如果下一个为null，表示到末尾了，退出迭代
                    continue;
                }
                else
                {
                    //如果下一个点在另一个曲线上，则插入一个非法点
                    if(tmpNext.CurveIdx != tmpCur.CurveIdx)
                    {
                        BezierPointListAdd(invalidPoint);
                    }
                    if (tmpNext.IsOn)
                    {//表示该控制点在曲线上，为线段
                        if(!tmpNext.Equals(tmpCur))
                        {//如果下一个和当前的不重复，才添加
                            BezierPointListAdd(tmpNext.Pos);
                        }
                    }
                    else
                    {//表示该控制点在曲线外，为曲线
                        //获取下一个的下一个
                        TTFCharInfoContourPT tmpNextNext = null;
                        if (i + 2 < controlPoints.Count)
                        {
                            tmpNextNext = controlPoints[i + 2];
                        }
                        if(tmpNextNext == null)
                        {
                            Debug.LogError("夭寿啦！tmpNextNext == null");
                            continue;
                        }
                        int curIdx = 1;
                        while (curIdx < Smooth)
                        {
                            float rate = curIdx * 1.0f / Smooth;
                            Vector2 tmpVec = GetQuadraticBezierPoint(tmpCur.Pos, tmpNext.Pos, tmpNextNext.Pos, rate);
                            //Debug.Log(string.Format("{0},{1},{2}", curIdx, tmpVec, rate));
                            BezierPointListAdd(tmpVec);
                            curIdx++;
                        }
                    }
                }
            }
        }

        //实例化控制点标记
        bool isChangeColor = false;
        for (int i = tranControlPoints.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(tranControlPoints.GetChild(i).gameObject);
        }
        for (int i = 0; i < controlPoints.Count; i++)
        {
            Image img = GameObject.Instantiate<Image>(imgPointTpl, tranControlPoints);
            img.gameObject.SetActive(true);
            TTFCharInfoContourPT tmpTTFCharInfoContourPT = controlPoints[i];
            Debug.Log(string.Format("控制点 - {0},{1},{2}", i, tmpTTFCharInfoContourPT.Pos, tmpTTFCharInfoContourPT.IsOn));
            if (IsInValidPoint(tmpTTFCharInfoContourPT))
            {
                isChangeColor = !isChangeColor;
                continue;
            }
            img.rectTransform.position = tmpTTFCharInfoContourPT.Pos;
            if (isChangeColor)
            {
                img.color = Color.black;
            }
            else
            {
                img.color = Color.white;
            }
        }
        //贝塞尔点
        for (int i = 0; i < BezierPointList.Count; i++)
        {
            if (BezierPointList[i] != invalidPoint)
            {
                Debug.Log(string.Format("贝塞尔点 - {0},{1}", i,BezierPointList[i]));
            }
        }
    }

    private bool IsInValidPoint(TTFCharInfoContourPT pTTFCharInfoContourPT)
    {
        bool isInvalid = pTTFCharInfoContourPT.Pos.x < 0;
        if (isInvalid)
        {
            Debug.LogError(string.Format("IsInValidPoint - {0}", pTTFCharInfoContourPT.Pos));
        }
        return isInvalid;
    }

    private void BezierPointListAdd(Vector2 v2)
    {
        if(BezierPointList.Count > 0)
        {
            if(BezierPointList[BezierPointList.Count - 1] != v2)
            {
                BezierPointList.Add(v2);
            }
        }
        else
        {
            BezierPointList.Add(v2);
        }
    }
}

internal class TTFCharInfo
{//TTGlyph
    internal string Name { get; private set; }    //name
    internal Rect Rect { get; private set; } //xMin yMin xMax yMax
    internal List<TTFCharInfoContour> ContourList { get; private set; }   //contour

    private TTFCharInfo() { }
    internal static TTFCharInfo Create(string name, int xMin, int yMin, int xMax, int yMax, List<TTFCharInfoContour> list)
    {
        TTFCharInfo tmpInstance = new TTFCharInfo();
        tmpInstance.Name = name;
        tmpInstance.Rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        tmpInstance.ContourList = list;
        return tmpInstance;
    }
}

internal class TTFCharInfoContour
{//contour
    internal List<TTFCharInfoContourPT> PTList { get; private set; }  //pt
    private TTFCharInfoContour() { }
    internal static TTFCharInfoContour Create(List<TTFCharInfoContourPT> pts)
    {
        TTFCharInfoContour tmpInstance = new TTFCharInfoContour();
        tmpInstance.PTList = pts;
        return tmpInstance;
    }
}

internal class TTFCharInfoContourPT
{//pt
    internal int CurveIdx { get; set; }   //该点所在的曲线索引
    internal Vector2 Pos { get; set; }    //x y
    internal bool IsOn { get; private set; } //on

    private TTFCharInfoContourPT() { }
    internal static TTFCharInfoContourPT Create(int x, int y, int on, int curveIdx)
    {
        TTFCharInfoContourPT tmpInstance = new TTFCharInfoContourPT();
        tmpInstance.Pos = new Vector2(x, y);
        tmpInstance.IsOn = on == 0 ? false : true;
        tmpInstance.CurveIdx = curveIdx;
        return tmpInstance;
    }
}