using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
	public sealed class FoodNetworkTests
	{
		private readonly List<GameObject> _objects = new();
		private readonly List<NetworkManager> _managers = new();
		private NetworkManager Node(string name, ushort port)
		{
			var obj = new GameObject(name);
			_objects.Add(obj);
			NetworkManager manager = obj.AddComponent<NetworkManager>();
			UnityTransport transport = obj.AddComponent<UnityTransport>();
			transport.SetConnectionData("127.0.0.1", port);
			manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport };
			manager.NetworkConfig.EnableSceneManagement = false;
			manager.NetworkConfig.ForceSamePrefabs = false;
			_managers.Add(manager);
			return manager;
		}
		private FoodSpawner Food(NetworkManager manager)
		{
			var obj = new GameObject("Food table " + manager.name);
			_objects.Add(obj);
			FoodPool config = obj.AddComponent<FoodPool>();
			FoodSpawner spawner = obj.AddComponent<FoodSpawner>();
			spawner.Initialize(config, manager);
			return spawner;
		}
		private static IEnumerator Wait(System.Func<bool> condition)
		{
			float end = Time.realtimeSinceStartup + 15f;
			while (!condition() && Time.realtimeSinceStartup < end) yield return null;
			Assert.That(condition(), Is.True, "Food replication did not converge before timeout.");
		}

		[UnityTest, Timeout(60000)]
		public IEnumerator LateJoinWaitsForFoodHandlerAndRepairsRevisionGaps()
		{
			ushort port;
			using (UdpClient socket = new(0))
			{
				port = (ushort)((IPEndPoint)socket.Client.LocalEndPoint).Port;
			}

			NetworkManager host = Node("Food baseline host", port);
			Assert.That(host.StartHost(), Is.True);
			FoodSpawner server = Food(host);
			const int recordCount = 2000;
			for (int index = 0; index < recordCount; index++)
			{
				server.CreateObject();
			}

			server.MarkWorldReady();
			FoodReplication hostReplication = Replication(server);
			NetworkManager remote = Node("Food baseline late joiner", port);
			Assert.That(remote.StartClient(), Is.True);
			yield return Wait(() => remote.IsConnectedClient);

			yield return new WaitForSecondsRealtime(2.25f);
			Assert.That(hostReplication.SentPackets, Is.Zero, "The host sent food before the client's Game-scene handler was ready.");

			FoodSpawner client = Food(remote);
			yield return Wait(() => client.IsReplicaReady);
			yield return new WaitForSecondsRealtime(2.25f);
			Assert.That(client.World.ActiveCount, Is.EqualTo(recordCount));
			const int snapshotPackets = 2 + (recordCount + FoodPacketWriter.RecordsPerPacket - 1) / FoodPacketWriter.RecordsPerPacket;
			Assert.That(hostReplication.SentPackets, Is.EqualTo(snapshotPackets), "A late join should send one complete baseline.");
			TestContext.WriteLine($"One food baseline: {hostReplication.SentPackets} packets, {hostReplication.SentBytes} payload bytes.");

			FoodReplica replica = Replication(client).Replica;
			FoodPacketWriter gap = new();
			gap.Reset(FoodMessage.Delta, replica.Epoch, replica.Revision + 2);
			gap.Byte(0);
			replica.Receive(gap.Buffer, gap.Length);
			Assert.That(replica.NeedsSnapshot, Is.True);

			yield return Wait(() => !replica.NeedsSnapshot);
			Assert.That(replica.World.ActiveCount, Is.EqualTo(recordCount));
			Assert.That(hostReplication.SentPackets, Is.EqualTo(2 * snapshotPackets), "A revision gap should send one repair baseline.");

			ulong newEpoch = hostReplication.Epoch + 1;
			hostReplication.StartEpoch(newEpoch);
			yield return Wait(() => replica.Epoch == newEpoch && !replica.NeedsSnapshot);
			Assert.That(replica.World.ActiveCount, Is.EqualTo(recordCount));
			Assert.That(hostReplication.SentPackets, Is.EqualTo(3 * snapshotPackets), "An epoch change should rebaseline the subscribed client once.");
		}

		private static FoodReplication Replication(FoodSpawner spawner)
		{
			FieldInfo field = typeof(FoodSpawner).GetField("_replication", BindingFlags.Instance | BindingFlags.NonPublic);

			return (FoodReplication)field.GetValue(spawner);
		}

		[UnityTest, Timeout(60000)]
		public IEnumerator BinarySnapshotsDeltasLateJoinAndIdleTrafficWorkOverNgoTransport()
		{
			ushort port;
			using (var socket = new UdpClient(0)) port = (ushort)((IPEndPoint)socket.Client.LocalEndPoint).Port;
			NetworkManager host = Node("Food test host", port);
			Assert.That(host.StartHost(), Is.True);
			FoodSpawner server = Food(host);
			for (int i = 0; i < 2000; i++) server.CreateObject();
			server.MarkWorldReady();
			NetworkManager remote = Node("Food test client", port);
			Assert.That(remote.StartClient(), Is.True);
			FoodSpawner client = Food(remote);
			yield return Wait(() => remote.IsConnectedClient && client.IsReplicaReady);
			Assert.That(client.World.ActiveCount, Is.EqualTo(2000));
			Assert.That(host.SpawnManager.SpawnedObjects.Count, Is.Zero, "Food must not spawn individual NGO objects.");
			FoodRecord first = server.World.At(0);
			first.Position = Vector2.one;
			yield return Wait(() => client.World.Find(first.Id)?.Position == first.Position);

			NetworkManager late = Node("Food test late joiner", port);
			Assert.That(late.StartClient(), Is.True);
			FoodSpawner lateClient = Food(late);
			yield return Wait(() => late.IsConnectedClient && lateClient.IsReplicaReady);
			Assert.That(lateClient.World.ActiveCount, Is.EqualTo(2000));
			ulong old = first.Id;
			server.Respawn(first);
			yield return Wait(() => client.World.Find(first.Id) != null && lateClient.World.Find(first.Id) != null);
			Assert.That(client.World.Find(old), Is.Null);
			Assert.That(lateClient.World.Find(first.Id).Position, Is.EqualTo(first.Position));
			FoodReplication replication = Replication(server);
			yield return new WaitForSecondsRealtime(0.3f);
			int packets = replication.SentPackets;
			yield return new WaitForSecondsRealtime(0.4f);
			Assert.That(replication.SentPackets, Is.EqualTo(packets), "Idle food must send no per-item updates.");
			Debug.Log($"Food replication: 2000 records, host and two clients, payload sent={replication.SentBytes} bytes; idle packets=0.");
		}

		[UnityTearDown]
		public IEnumerator Cleanup()
		{
			foreach (NetworkManager manager in _managers) if (manager != null) manager.Shutdown();
			for (int i = 0; i < 5; i++) yield return null;
			for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i] != null) Object.Destroy(_objects[i]);
			_objects.Clear(); _managers.Clear();
			yield return null;
		}
	}
}
