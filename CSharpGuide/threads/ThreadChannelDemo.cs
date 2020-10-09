using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CSharpGuide.threads
{
    public class ThreadChannelDemo
    {
        public static async void Start()
        {
            // 创建一个无限制容量的通讯通道
            var unlimitChannel = Channel.CreateUnbounded<string>(); // 这种当消费者速度跟不上生产者速度时，这很危险，因为最后会因为生产者队列越积越多导致内存耗尽
            // 写进数据（生产数据）
            await unlimitChannel.Writer.WriteAsync("New Message!");
            // 从通道中读取数据
            await ReadAsync(unlimitChannel);
        }

        private static async Task ReadAsync(Channel<string> unlimitChannel)
        {
            while (await unlimitChannel.Reader.WaitToReadAsync())
            {
                if(unlimitChannel.Reader.TryRead(out var msg))
                {
                    Console.WriteLine($"消费者读取数据：{msg}");
                }
            }
        }

        public static async Task SingleProducerSingleConsumer()
        {
            var channel = Channel.CreateUnbounded<string>();
            // 这里设置消费者要比生产者速度快
            var consumer = new Consumer(channel.Reader, 1, 1200);
            var producer = new Producer(channel.Writer, 1, 1500);

            var consumerTask = consumer.ConsumeData();
            var producerTask = producer.BeginProducing();

            await producerTask.ContinueWith(_ => channel.Writer.Complete());
            await consumerTask;
        }

        public static async Task MultipleProducerSingleConsumer()
        {
            var channel = Channel.CreateUnbounded<string>();
            var consumer = new Consumer(channel.Reader, 1, 200);

            var producer1 = new Producer(channel.Writer, 1, 1000);
            var producer2 = new Producer(channel.Writer, 1, 1000);
            var producer3 = new Producer(channel.Writer, 1, 1000);

            var consumerTask = consumer.ConsumeData();
            var producerTask1 = producer1.BeginProducing();
            var producerTask2 = producer2.BeginProducing();
            var producerTask3 = producer3.BeginProducing();

            await Task.WhenAll(producerTask1, producerTask2, producerTask3)
                .ContinueWith(_ => channel.Writer.Complete());
            await consumerTask;
        }


        internal class Consumer
        {
            private readonly ChannelReader<string> _reader;
            private readonly int _identifier;
            private readonly int _delay;

            public Consumer(ChannelReader<string> reader, int identifier, int delay)
            {
                _reader = reader;
                _identifier = identifier;
                _delay = delay;
            }
            public async Task ConsumeData()
            {
                Console.WriteLine($"CONSUMER ({_identifier}): Starting");

                while (await _reader.WaitToReadAsync())
                {
                    if (_reader.TryRead(out var timeString))
                    {
                        await Task.Delay(_delay); // simulate processing time

                        Console.WriteLine($"CONSUMER ({_identifier}): Consuming {timeString}");
                    }
                }

                Console.WriteLine($"CONSUMER ({_identifier}): Completed");
            }
        }

        internal class Producer
        {
            private readonly ChannelWriter<string> _writer;
            private readonly int _identifier;
            private readonly int _delay;

            public Producer(ChannelWriter<string> writer, int identifier, int delay)
            {
                _writer = writer;
                _identifier = identifier;
                _delay = delay;
            }
            public async Task BeginProducing()
            {
                Console.WriteLine($"PRODUCER ({_identifier}): Starting");

                for (var i = 0; i < 10; i++)
                {
                    await Task.Delay(_delay); // simulate producer building/fetching some data

                    var msg = $"P{_identifier} - {DateTime.UtcNow:G}";

                    Console.WriteLine($"PRODUCER ({_identifier}): Creating {msg}");

                    await _writer.WriteAsync(msg);
                }

                Console.WriteLine($"PRODUCER ({_identifier}): Completed");
            }
        }
    }
}
