        /// <summary>
        /// 缓存串口接收的数据
        /// </summary>
        private static List<byte> ReceiveData = new List<byte>();

        /// <summary>
        /// 假如张图片为1000个字节
        /// </summary>
        private const int FrameLength = 1000;

        /// <summary>
        /// 定义一个信号量
        /// </summary>
        private static AutoResetEvent event_1 = new AutoResetEvent(true);

        /// <summary>
        /// 运行状态
        /// </summary>
        private static bool IsRun = false;

        //用于测试时计数
        private static int Count = 0;

        private static int SendCount = 0;
        private static int ReceiveCount = 0;

        private static void Main(string[] args)
        {
      

            //============启动软件初始化时===========
			IsRun = true;
            //创建线程
            var t = Task.Factory.StartNew(() =>
              {
                  try
                  {
                      ThreadProcess(ParseImage);
                  }
                  catch (Exception ex)
                  {
                      Console.WriteLine(ex.Message + ex.ToString());
                  }
              });
			          
			//============开个线程模拟串接收到的数据===========
            Thread.Sleep(100); //尽量先让ThreadProcess线程先启动，然后再开始接收数据    
            Task.Factory.StartNew(() =>
            {
                //模拟接口将要接收10000条数据
                for (int i = 0; i < 100; i++)
                {
                    byte[] data = Encoding.ASCII.GetBytes("testafdasfdasfjkloi;l453");
                    sp_DateReceived(data);
                }
                //多线程情况下，下面打印的数据可能存在误差
                Console.WriteLine($"---数据上传完成，串口接收{SendCount}字节数据，已经处理的{Count}帧/条\r\n处理Count*FrameLength={ReceiveCount}字节，还剩下约{ReceiveData.Count}字节正在处理-------");
            });

            Console.ReadLine();

            //==========停止/关闭软件前========

            IsRun = false;
            //避免卡在  WaitOne处
            event_1.Set();

            //等待线程处理完成
            t.Wait();

            Console.WriteLine($"--------发送字节数：{SendCount},接收字节数：{ReceiveCount}-------");

            Console.ReadLine();
        }

        /// <summary>
        /// 处理图片的方法
        /// </summary>
        /// <param name="data"></param>
        private static void ParseImage(byte[] data)
        {
            //1、处理或者存储图片

            //2、更新UI
            /*
            this.Invoke(new EventHandler(delegate
            {
                //更新UI，线程中操作UI不安全，需要在委托中更新UI
            }));
            */

            //3、如果处理仍然不过来，还可以开线程池，只要电脑配置不太差，不过桌面软件没有必要搞这么复杂
        }

        /// <summary>
        /// 线程
        /// </summary>
        /// <param name="fun">这个参数可以不要，直接在fun处调用方法ParseImage</param>
        private static void ThreadProcess(Action<byte[]> fun)
        {
            //ReceiveData.Count >= FrameLengthy主要是防止停止的时候，ReceiveData中的数据没有处理完
            while (IsRun/* || ReceiveData.Count >= FrameLength*/)
            {
                try
                {
                    if (ReceiveData.Count < FrameLength)
                    {
                        //等待通知信号到来
                        event_1.WaitOne();
                    }
                    else //确保 ReceiveData.Count>=FrameLength  ，否则报错
                    {
                        byte[] data = new byte[FrameLength];
                        lock (ReceiveData)
                        {
                            Buffer.BlockCopy(ReceiveData.ToArray(), 0, data, 0, data.Length);
                            ReceiveData.RemoveRange(0, FrameLength);
                            ReceiveCount += FrameLength;
                        }
                        //处理data中的数据，也可以直接调用 ParseImage
                        fun(data);

                        Console.WriteLine($"--------未处理的字节数：{ReceiveData.Count}，处理次数：{++Count}----{ReceiveCount}-------");
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            //要添加上没有处理的
            ReceiveCount += ReceiveData.Count;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data">串口数据</param>
        private static void sp_DateReceived(byte[] data)

        {
            if (!IsRun)
            {
                return;
            }
            //...
            lock (ReceiveData)
            {
                ReceiveData.AddRange(data);
            }

            SendCount += data.Length;
            //串口接收的数据达到FrameLength时，说明这个数据是完整的，可以处理
            if (ReceiveData.Count >= FrameLength)
            {
                //通知线程处理数据
                event_1.Set();
            }
        }
