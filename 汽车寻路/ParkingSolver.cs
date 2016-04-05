using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Linq;

namespace WpfApplication1
{
    class ParkingSolver
    {
        /// <summary>
        /// 记录这个状态在第几步被访问到。
        /// <para>如果第10步就到了这个状态，那么禁止第20步重回。但是允许有更短的路径在第5步到达这个状态。</para>
        /// </summary>
        private readonly int[, ,] visitedState;

        public ParkingSolver(double distanceTolerance, double orientationTolerance, Point endPoint, double endOrientation, Map map)
        {
            DistanceTolerance = distanceTolerance;
            OrientationTolerance = orientationTolerance;
            EndPoint = endPoint;
            EndOrientation = endOrientation;
            Map = map;

            int a = (int)(map.Size.Width / distanceTolerance);
            int b = (int)(map.Size.Height / distanceTolerance);
            int c = (int)(360 / orientationTolerance);
            visitedState = new int[a, b, c];

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            for (int aa = 0; aa < a; aa++)
            {
                for (int bb = 0; bb < b; bb++)
                {
                    for (int cc = 0; cc < c; cc++)
                    {
                        visitedState[aa, bb, cc] = int.MaxValue;
                    }
                }
            }
            stopwatch.Stop();
            Console.WriteLine("矩阵初始化完成：" + stopwatch.ElapsedMilliseconds);

        }



        /// <summary> 获取求解距离的精确度 </summary>
        public double DistanceTolerance { get; private set; }
        /// <summary> 获取求解角度的精确度 </summary>
        public double OrientationTolerance { get; private set; }

        public Map Map { get; private set; }
        //public Car Car { get; private set; }

        public Point EndPoint { get; private set; }
        public double EndOrientation { get; private set; }

        internal LinkedList<Action> Solve(Car car)
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            var list = SolveInternal(car, 0, 100);
            stopwatch.Stop();
            Console.WriteLine("找到解：" + stopwatch.ElapsedMilliseconds);
            return CombineAction(list);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="car"></param>
        /// <param name="currentSteps">已经走了多少步。从0开始。</param>
        /// <param name="maxStepsToSolution">最多走多少步</param>
        /// <returns>如果无解，返回null。</returns>
        private LinkedList<Action> SolveInternal(Car car, int currentSteps, int maxStepsToSolution)
        {
            if (currentSteps >= maxStepsToSolution)
                return null;

            if ((car.Center - EndPoint).Length <= DistanceTolerance &&
                Math.Abs(car.Orientation - EndOrientation) <= OrientationTolerance)
            {
                Console.WriteLine("第{0}步找到解，寻找更优的解……", currentSteps);
                return new LinkedList<Action>();
            }

            double distance;
            double degree;

            bool enlargedGrid = false;
            //double d = GetNearestDistanceToFixure(car.Center);

            //enlargedGrid = d >= car.Length * 3;
            //if (enlargedGrid)
            //{
            //    distance = DistanceTolerance * 3;
            //    degree = OrientationTolerance * 3;
            //}
            //else
            //{
                distance = DistanceTolerance*2;//也可以不乘以2。
                degree = OrientationTolerance * 2;//也可以不乘以2。
            //}




            LinkedList<Action> bestSolution = null;


            //前进
            LinkedList<Action> actions = TestAction(car.Forward, ActionDirection.Forward, distance, currentSteps, maxStepsToSolution, enlargedGrid);
            if (actions != null)
            {
                maxStepsToSolution = actions.Count;
                bestSolution = actions;
            }

            //前进左转
            actions = TestAction(car.ForwardLeft, ActionDirection.ForwardLeft, degree, currentSteps, maxStepsToSolution, enlargedGrid);
            if (actions != null && actions.Count < maxStepsToSolution)
            {
                maxStepsToSolution = actions.Count;
                bestSolution = actions;
            }

            //前进右转
            actions = TestAction(car.ForwardRight, ActionDirection.ForwardRight, degree, currentSteps, maxStepsToSolution, enlargedGrid);
            if (actions != null && actions.Count < maxStepsToSolution)
            {
                maxStepsToSolution = actions.Count;
                bestSolution = actions;
            }

            //后退
            actions = TestAction(car.Backward, ActionDirection.Backward, distance, currentSteps, maxStepsToSolution, enlargedGrid);
            if (actions != null && actions.Count < maxStepsToSolution)
            {
                maxStepsToSolution = actions.Count;
                bestSolution = actions;
            }


            //后退左转
            actions = TestAction(car.BackwardLeft, ActionDirection.BackwardLeft, degree, currentSteps, maxStepsToSolution, enlargedGrid);
            if (actions != null && actions.Count < maxStepsToSolution)
            {
                maxStepsToSolution = actions.Count;
                bestSolution = actions;
            }


            //后退右转
            actions = TestAction(car.BackwardRight, ActionDirection.BackwardRight, degree, currentSteps, maxStepsToSolution, enlargedGrid);
            if (actions != null && actions.Count < maxStepsToSolution)
            {
                //maxStepsToSolution = actions.Count;
                bestSolution = actions;
            }

            return bestSolution;//如果无解，返回null。
        }

        private double GetNearestDistanceToFixure(Point center)
        {
            double[] d = new double[4];

            d[0] = center.X;
            d[1] = center.Y;
            d[2] = Map.Size.Width - center.X;
            d[3] = Map.Size.Height - center.Y;
            double distance = d.Min();

            d = new double[5];

            foreach (RectangleGeometry g in Map.Fixtures)
            {
                Point topLeft = g.Transform.Transform(g.Rect.TopLeft);
                Point topRight = g.Transform.Transform(g.Rect.TopRight);
                Point bottomLeft = g.Transform.Transform(g.Rect.BottomLeft);
                Point bottomRight = g.Transform.Transform(g.Rect.BottomRight);

                d[0] = Math2D.GetLineToPointDistance(topLeft, topRight, center, true);
                d[1] = Math2D.GetLineToPointDistance(topRight, bottomRight, center, true);
                d[2] = Math2D.GetLineToPointDistance(bottomRight, bottomLeft, center, true);
                d[3] = Math2D.GetLineToPointDistance(bottomLeft, topLeft, center, true);
                d[4] = distance;

                distance = d.Min();
            }

            return distance;
        }

        /// <summary>
        /// 如果无解返回null。
        /// </summary>
        /// <param name="act"></param>
        /// <param name="name"></param>
        /// <param name="parameter"></param>
        /// <param name="currentSteps"></param>
        /// <param name="maxStepsToSolution"></param>
        /// <param name="enlargedGrid"></param>
        /// <returns></returns>
        private LinkedList<Action> TestAction(Func<double, Car> act, ActionDirection name, double parameter, int currentSteps, int maxStepsToSolution, bool enlargedGrid)
        {
            Car vc = act(parameter);

            currentSteps++; //执行好act，所以又走了一步了！
            Point center = vc.Center;
            if (center.X > 0 && center.Y > 0 &&
                center.X < Map.Size.Width && center.Y < Map.Size.Height
                && IsShorterPath(center, vc.Orientation, currentSteps, enlargedGrid))
            {

                //不会碰到其他车
                if (DoesCollide(vc.GetBound()) == false)
                {
                    var list = SolveInternal(vc, currentSteps, maxStepsToSolution);
                    if (list != null) //有解
                    {
                        list.AddFirst(new Action { ActionDirection = name, Parameter = parameter });

                        //if(bestSolution==null)
                        //    bestSolution=list;


                        return list;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 现在是不是用更短的路径访问到这个节点。并且更新到这个节点的步数。
        /// </summary>
        /// <param name="center"></param>
        /// <param name="orientation"></param>
        /// <param name="currentSteps"></param>
        /// <param name="enlargedGrid"></param>
        /// <returns></returns>
        private bool IsShorterPath(Point center, double orientation, int currentSteps, bool enlargedGrid)
        {
            int a = (int)(center.X / DistanceTolerance);
            int b = (int)(center.Y / DistanceTolerance);
            int c = (int)(orientation / OrientationTolerance);
            if (enlargedGrid)
            {
                int[] n = new int[27];
                int index = 0;
                for (int aa = a - 1; aa <= a + 1; aa++)
                {
                    for (int bb = b - 1; bb <= b + 1; bb++)
                    {
                        //a,b表示x，y坐标，不会超出边界；因为如果接近边界，不会启动动态格点。
                        //但是c表示方向，可能超出边界。

                        for (int cc = c - 1; cc <= c + 1; cc++)
                        {
                            if (cc == -1)
                                n[index] = visitedState[aa, bb, visitedState.GetUpperBound(2)];
                            else if (cc == visitedState.GetUpperBound(2) + 1)
                                n[index] = visitedState[aa, bb, 0];
                            else
                                n[index] = visitedState[aa, bb, cc];

                            index++;
                        }
                    }
                }
                System.Diagnostics.Debug.Assert(index == 27);

                int min = n.Min();
                if (min > currentSteps)//以前套远路，现在可以用更短的路径访问到
                {
                    for (int aa = a - 1; aa <= a + 1; aa++)
                    {
                        for (int bb = b - 1; bb <= b + 1; bb++)
                        {
                            for (int cc = c - 1; cc <= c + 1; cc++)
                            {
                                if (cc == -1)
                                    visitedState[aa, bb, visitedState.GetUpperBound(2)] = currentSteps;
                                else if (cc == visitedState.GetUpperBound(2) + 1)
                                    visitedState[aa, bb, 0] = currentSteps;
                                else
                                    visitedState[aa, bb, cc] = currentSteps;
                            }
                        }
                    }
                    return true;
                }
                else
                    return false;
            }
            else
            {
                if (visitedState[a, b, c] > currentSteps)//以前套远路，现在可以用更短的路径访问到
                {
                    visitedState[a, b, c] = currentSteps;
                    return true;
                }
                else
                    return false;
            }
        }

        private bool DoesCollide(Geometry carBound)
        {
            foreach (var fixture in Map.Fixtures)
            {
                if (carBound.FillContainsWithDetail(fixture) != IntersectionDetail.Empty)
                    return true;
            }
            return false;
        }

        internal LinkedList<Action> CombineAction(LinkedList<Action> actions)
        {
            LinkedList<Action> combinedActions = new LinkedList<Action>();
            Action combinedAction = new Action();
            combinedAction.ActionDirection = actions.First.Value.ActionDirection;
            combinedAction.Parameter = 0;
            foreach (Action action in actions)
            {
                if (combinedAction.ActionDirection == action.ActionDirection)
                    combinedAction.Parameter += action.Parameter;
                else //动作序列完成
                {
                    combinedActions.AddLast(combinedAction);

                    combinedAction = new Action();
                    combinedAction.ActionDirection = action.ActionDirection;
                    combinedAction.Parameter = action.Parameter;
                }
            }
            combinedActions.AddLast(combinedAction);

            return combinedActions;
        }
    }
}
