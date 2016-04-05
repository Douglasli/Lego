using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using System.Windows.Media.Media3D;
using Gqqnbig.Linq;

namespace Gqqnbig.Lego
{
    public class ParkingSolver
    {
        ///// <summary>
        ///// 记录这个状态在第几步被访问到。
        ///// <para>如果第10步就到了这个状态，那么禁止第20步重回。但是允许有更短的路径在第5步到达这个状态。</para>
        ///// </summary>
        //private readonly int[, ,] visitedState;

        public ParkingSolver(double distanceTolerance, double orientationTolerance, Point endPoint, double endOrientation, Map map)
        {
            DistanceTolerance = distanceTolerance;
            OrientationTolerance = orientationTolerance;
            EndPoint = endPoint;
            EndOrientation = endOrientation;
            Map = map;

            Close_List = new List<Node>[(int)(360 / orientationTolerance / 2)];
            for (int i = 0; i < Close_List.Length; i++)
            {
                Close_List[i] = new List<Node>();
            }

        }


        /// <summary> 获取求解距离的精确度。汽车会以两倍容差前进；但由于旋转，实际到达的位置是连续的。 </summary>
        /// <remarks>
        /// 距离容差t相当于对xy平面划分了格点。
        /// 设目标位置的x（或y）值为a，当前车辆位置为b，如果a-b &lt;=t，那么认为当前车和目标车在同一格。
        /// <para>如果步进长度等于格点长度，当车辆对齐x轴或y轴时，仅仅使用前进后退两种动作，就可以到达直线方向上的任何格点。
        /// 但实际上网格是有稍微移动的。设目标车坐标为(x,y)，所在格点为(X,Y)，网格可上下左右移动，只要X&lt;=x&ltX+t。
        /// 所以步进长度2t最好。
        /// </para>
        /// </remarks>
        public double DistanceTolerance { get; private set; }
        /// <summary> 获取求解角度的精确度。汽车会以两倍容差旋转；并且实际到达的朝向是以两倍容差相间的。 </summary>
        public double OrientationTolerance { get; private set; }

        public Map Map { get; private set; }
        ////public VehicleState VehicleState { get; private set; }

        public Point EndPoint { get; private set; }
        public double EndOrientation { get; private set; }

        /// <summary>
        /// 以z为索引的开放列表。
        /// <para>用<code>Position.Z/orientationTolerance/2</code>来访问。</para>
        /// </summary>
        internal LinkedList<Node> Open_List = new LinkedList<Node>();

        /// <summary>
        /// 以z为索引的封闭列表。
        /// <para>用<code>Position.Z/orientationTolerance/2</code>来访问。</para>
        /// </summary>
        internal List<Node>[] Close_List;
        private bool pauseRequired;
        private readonly Semaphore pauseRequiredSemaphore = new System.Threading.Semaphore(0, 1);
        private readonly Semaphore resumeSemaphore = new Semaphore(0, 1);
        private bool isRunning;

        public LinkedList<VehicleAction> Solve(VehicleState car)
        {
            isRunning = true;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var list = Solve(new Point3D(car.Center.X, car.Center.Y, car.Orientation),
                new Point3D(EndPoint.X, EndPoint.Y, EndOrientation));
            stopwatch.Stop();

            isRunning = false;

            if (list == null)
                Console.WriteLine("无解：" + stopwatch.ElapsedMilliseconds / 1000);
            else
                Console.WriteLine("找到解：" + stopwatch.ElapsedMilliseconds / 1000);

#if DEBUG
            Console.WriteLine("启发函数与实际值的方差为{0:F2}", huristicDifferences.Select(n => (double)n).Variance());
#endif

            return CombineAction(list);
        }


        private bool DoesCollide(Node node)
        {
            RectangleGeometry carBound = node.Car.GetBound();

            foreach (var fixture in Map.Fixtures)
            {
                if (carBound.FillContainsWithDetail(fixture) != IntersectionDetail.Empty)
                    return true;
            }
            return false;
        }

        internal LinkedList<VehicleAction> CombineAction(LinkedList<VehicleAction> actions)
        {
            LinkedList<VehicleAction> combinedActions = new LinkedList<VehicleAction>();
            VehicleAction combinedAction = new VehicleAction();
            combinedAction.ActionDirection = actions.First.Value.ActionDirection;
            combinedAction.Parameter = 0;
            foreach (VehicleAction action in actions)
            {
                if (combinedAction.ActionDirection == action.ActionDirection)
                    combinedAction.Parameter += action.Parameter;
                else //动作序列完成
                {
                    combinedActions.AddLast(combinedAction);

                    combinedAction = new VehicleAction();
                    combinedAction.ActionDirection = action.ActionDirection;
                    combinedAction.Parameter = action.Parameter;
                }
            }
            combinedActions.AddLast(combinedAction);

            return combinedActions;
        }





        //从开启列表查找F值最小的节点
        private Node GetMinFFromOpenList()
        {
            var minNode = Open_List.First.Value;
            foreach (var p in Open_List)
            {
                if (minNode.TotalCost > p.TotalCost)
                    minNode = p;
            }
            return minNode;
        }


        //判断关闭列表是否包含一个坐标的点
        private bool IsInCloseList(Point3D nodePosition)
        {
            List<Node> sublist = Close_List[(int)(nodePosition.Z / OrientationTolerance / 2)];


            return sublist.Any(p => Point3DEquals(p.Position, nodePosition, DistanceTolerance, OrientationTolerance));
        }

        /// <summary>
        /// 用指定的精度比较两个三维空间中的点
        /// </summary>
        /// <returns></returns>
        private static bool Point3DEquals(Point3D p1, Point3D p2, double distanceTolerance, double orientationTolerance)
        {
            return System.Math.Abs(p2.X - p1.X) < distanceTolerance && System.Math.Abs(p2.Y - p1.Y) < distanceTolerance && System.Math.Abs(p2.Z - p1.Z) < orientationTolerance;
        }

        //判断开启列表是否包含一个坐标的点
        private Node GetFromOpenList(Node node)
        {
            return GetFromOpenList(node.Position);
        }

        private Node GetFromOpenList(Point3D position)
        {
            return Open_List.FirstOrDefault(p => Point3DEquals(p.Position, position, DistanceTolerance, OrientationTolerance));
        }

        #region 曼哈顿算法求开销

        protected int GetForecastCost(Node node, Node endNode)
        {

            Vector3D v = node.Position - endNode.Position;
            v.Z = Gqqnbig.Math.WrapAngle360(v.Z);
            if (v.Z > 180)
                v.Z = 360 - v.Z;

            Debug.Assert(0 <= v.Z && v.Z <= 180, "两个角的差必须在[0,180]的范围内！");

            return (int)(v.Length);//*10是为了增加精确度。由于未知原因，如果开销乘以10，算法速度会慢3倍！
        }


        //计算某个点的G值
        private int GetG(Node node)
        {
            if (node.Parent == null)
                return 0;
            Vector3D v = node.Position - node.Parent.Position;
            v.Z = Gqqnbig.Math.WrapAngle360(v.Z);
            if (v.Z > 180)
                v.Z = 360 - v.Z;
            Debug.Assert(0 <= v.Z && v.Z <= 180, "两个角的差必须在[0,180]的范围内！");

            return (int)(node.Parent.AccumulatedCost + v.Length);//由于未知原因，如果开销乘以10，算法速度会慢3倍！
        }
        #endregion

        #region 中心移动算法求开销

        //protected int GetForecastCost(Node node, Node endNode)
        //{

        //    Vector3D v = node.Position - endNode.Position;
        //    v.Z = Gqqnbig.Math.WrapAngle360(v.Z);
        //    if (v.Z > 180)
        //        v.Z = 360 - v.Z;
        //    Debug.Assert(0 <= v.Z && v.Z <= 180, "两个角的差必须在[0,180]的范围内！");

        //    //调整朝向
        //    //1.圆周运动
        //    double l1 = 2 * Math.PI * v.Z / 360;
        //    //2.圆周运动产生的平移
        //    double dx = node.VehicleState.TurningRadius * (1 - Math.Cos(v.Z / 180 * Math.PI));
        //    double dy = node.VehicleState.TurningRadius * Math.Sin(v.Z / 180 * Math.PI);

        //    double x = Math.Abs(Math.Abs(v.X) - dx);
        //    double y = Math.Abs(Math.Abs(v.Y) - dy);


        //    return (int)((Math.Sqrt(x * x + y * y) + l1) * 10);
        //}


        ////计算某个点的G值
        //private int GetG(Node node)
        //{
        //    if (node.Parent == null)
        //        return 0;
        //    Vector3D v = node.Position - node.Parent.Position;
        //    v.Z = Gqqnbig.Math.WrapAngle360(v.Z);
        //    if (v.Z > 180)
        //        v.Z = 360 - v.Z;

        //    double res;
        //    if (node.FromAction.ActionDirection == ActionDirection.Backward ||
        //        node.FromAction.ActionDirection == ActionDirection.Forward)
        //    {
        //        res = (int)(node.Parent.AccumulatedCost + node.FromAction.Parameter * 10);
        //    }
        //    else
        //    {
        //        double l = 2 * Math.PI * node.VehicleState.TurningRadius / 360 * v.Z;
        //        res = (int)(node.Parent.AccumulatedCost + l * 10);
        //    }

        //    if (res < 0)
        //    {

        //    }

        //    return (int)res;

        //    //return (int)(node.Parent.AccumulatedCost + v.Length * 10);
        //}

        #endregion


        //检查当前节点附近的节点
        private void CheckP8(Node p0, Node endNode)
        {
            List<Node> connectedNodes = p0.GetConnectedPositions(DistanceTolerance * 2, OrientationTolerance * 2);

            foreach (Node np in connectedNodes)
            {
                //if (np.Position.Z == 190)
                //{

                //}


                //排除超过边界的点
                if (np.Position.X > 0 && np.Position.X < Map.Size.Width &&
                    np.Position.Y > 0 && np.Position.Y < Map.Size.Height)
                {
                    //排除无法到达的点和关闭列表中的点
                    //无法到达的点是不会进入开放列表和封闭列表的，
                    //但可能不同的点经由不同的动作反复生成，
                    if (DoesCollide(np) == false && IsInCloseList(np.Position) == false)
                    {
                        Node openNode = GetFromOpenList(np.Position);
                        if (openNode != null)
                        {
                            np.AccumulatedCost = openNode.AccumulatedCost;
                            np.ForecastCost = openNode.ForecastCost;
                            np.Parent = p0;
                            //openNode = np;

                            //Vector3D v = p0.Position - openNode.Position;
                            //Debug.Assert(GetG(openNode) == (int)(p0.AccumulatedCost + v.Length * 10));
                            int newAccumulatedCost = GetG(np); //(int)(p0.AccumulatedCost + v.Length * 10);

                            if (newAccumulatedCost < np.AccumulatedCost)
                            {
                                np.Parent = p0;
                                np.AccumulatedCost = newAccumulatedCost;
                            }
                        }
                        else
                        {
                            //不在开放列表中
                            np.Parent = p0;
                            //Vector3D v = p0.Position - np.Position;
                            //Debug.Assert(GetG(np) == (int)(p0.AccumulatedCost + v.Length * 10));
                            int newAccumulatedCost = GetG(np); //(int)(p0.AccumulatedCost + v.Length * 10);
                            np.AccumulatedCost = newAccumulatedCost;
                            np.ForecastCost = GetForecastCost(np, endNode);
                            Open_List.AddLast(np);
                        }

                    }
                }
            }
        }

#if DEBUG
        /// <summary>
        /// 记录启发值与实际值之差。
        /// </summary>
        LinkedList<int> huristicDifferences = new LinkedList<int>();
#endif

        private LinkedList<VehicleAction> Solve(Point3D startPoint, Point3D endPoint)
        {
            var startNode = new Node(startPoint, null);
            var endNode = new Node(endPoint, null);


            Open_List.AddLast(startNode);

            int iterationCount = 0;


            while ((GetFromOpenList(endNode)) == null && Open_List.Count != 0)
            {
                if (pauseRequired)
                {
                    pauseRequiredSemaphore.Release();
                    pauseRequired = false;
                    resumeSemaphore.WaitOne();
                }


                var p0 = GetMinFFromOpenList();

                if (p0 == null)
                    return null;

                iterationCount++;

#if DEBUG
                double h = GetForecastCost(startNode, p0);
                if (h > p0.AccumulatedCost)
                    throw new InvalidOperationException(
                        string.Format("从起点{0}到点{1}的启发值为{2}，实际值为{3}。实际值小于启发值，算法有误，可能无法找到解！",
                            startNode.Position, p0.Position, h, p0.AccumulatedCost));
                else
                {
                    //记录启发与实际的差
                    if (System.Math.Abs(h - p0.AccumulatedCost) > 0.001)
                        huristicDifferences.AddLast((int)(p0.AccumulatedCost - h));
                }
#endif

                Open_List.Remove(p0);
                Close_List[(int)(p0.Position.Z / OrientationTolerance / 2)].Add(p0);
                CheckP8(p0, endNode);
            }

            Debug.WriteLine("迭代{0}次，封闭列表{1}项，开放列表{2}项。", iterationCount, Close_List.Sum(l => l.Count), Open_List.Count);


            LinkedList<VehicleAction> resultList = new LinkedList<VehicleAction>();
            endNode = GetFromOpenList(endNode);
            var p = endNode;
            Debug.Assert(Point3DEquals(p.Position, endPoint, DistanceTolerance, OrientationTolerance));
            while (p.Parent != null)
            {
                if (p.FromAction == null)
                    break;

                resultList.AddFirst(p.FromAction);
                //Console.WriteLine(p.Position + " <- " + p.FromAction);
                p = p.Parent;
                //R[p.Position.Y, p.Position.X] = 3;
            }
            Debug.Assert(Point3DEquals(p.Position, startPoint, DistanceTolerance, OrientationTolerance));
            return resultList;

        }

        /// <summary>
        /// 请求暂停当前算法。此方法不会返回，直到算法被暂停。
        /// </summary>
        public void RequirePause()
        {
            if (isRunning)
            {
                pauseRequired = true;
                pauseRequiredSemaphore.WaitOne();
            }
        }

        public void Resume()
        {
            resumeSemaphore.Release();
        }


        /// <summary>
        /// 求解过程中的节点。
        /// </summary>
        public class Node
        {
            private Node m_parent;


            public Node(Point3D position, VehicleAction from)
            {
                Position = position;
                FromAction = from;

                Car = new VehicleState(new Point(position.X, position.Y), position.Z);
            }

            public VehicleState Car { get; private set; }

            public Point3D Position { get; private set; }

            /// <summary>
            /// 获取这个状态是怎么来的，用于收集解。
            /// </summary>
            public VehicleAction FromAction { get; private set; }

            /// <summary>
            /// 从起点到当前点的开销
            /// </summary>
            public int AccumulatedCost { get; set; }

            /// <summary>
            /// 从当前点到目标点的估计开销
            /// </summary>
            public int ForecastCost { get; set; }

            public int TotalCost
            {
                get { return AccumulatedCost + ForecastCost; }
            }

            public Node Parent
            {
                get
                {

                    return m_parent;
                }
                set
                {
#if DEBUG

                    if (FromAction.ActionDirection == ActionDirection.BackwardRight)
                    {
                        double diff = System.Math.Abs(Position.Z - value.Position.Z);
                        if (diff != FromAction.Parameter && diff != 360 - FromAction.Parameter)
                        {
                            throw new InvalidOperationException("本节点声称由父节点经过旋转得到，但本节点跟父节点的朝向相同。");
                        }
                    }
#endif

                    m_parent = value;
                }
            }

            public List<Node> GetConnectedPositions(double distance, double degree)
            {
                List<Node> list = new List<Node>(6);

                VehicleState vc = Car.Forward(distance);
                list.Add(new Node(new Point3D(vc.Center.X, vc.Center.Y, vc.Orientation),
                    new VehicleAction { ActionDirection = ActionDirection.Forward, Parameter = distance }));

                vc = Car.ForwardLeft(degree);
                list.Add(new Node(new Point3D(vc.Center.X, vc.Center.Y, vc.Orientation),
                    new VehicleAction { ActionDirection = ActionDirection.ForwardLeft, Parameter = degree }));

                vc = Car.ForwardRight(degree);
                list.Add(new Node(new Point3D(vc.Center.X, vc.Center.Y, vc.Orientation),
                    new VehicleAction { ActionDirection = ActionDirection.ForwardRight, Parameter = degree }));

                vc = Car.Backward(distance);
                list.Add(new Node(new Point3D(vc.Center.X, vc.Center.Y, vc.Orientation),
                    new VehicleAction { ActionDirection = ActionDirection.Backward, Parameter = distance }));

                vc = Car.BackwardLeft(degree);
                list.Add(new Node(new Point3D(vc.Center.X, vc.Center.Y, vc.Orientation),
                    new VehicleAction { ActionDirection = ActionDirection.BackwardLeft, Parameter = degree }));

                vc = Car.BackwardRight(degree);
                //Debug.Assert(Math.Abs(VehicleState.Orientation - vc.Orientation) > 1);
                list.Add(new Node(new Point3D(vc.Center.X, vc.Center.Y, vc.Orientation),
                    new VehicleAction { ActionDirection = ActionDirection.BackwardRight, Parameter = degree }));

                return list;
            }
        }


    }
}
