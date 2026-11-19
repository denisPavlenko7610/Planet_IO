using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
	public sealed class FoodReplication : IDisposable
	{
		public const string MessageName = "PlanetIO.Food.v1";
		private const float SnapshotRetrySeconds = 2f;
		private readonly FoodSpawner _food;
		private readonly NetworkManager _manager;
		private readonly FoodPacketWriter _writer = new();
		private byte[] _receive = new byte[FoodPacketWriter.MaximumBytes];
		private readonly List<int> _dirty = new(2000);
		private readonly bool[] _marked = new bool[FoodWorld.MaximumRecords];
		private readonly List<FoodEatEvent> _effects = new(128);
		private readonly Dictionary<ulong, float> _sentSnapshots = new();
		private ulong _revision, _snapshot;
		private float _flush, _request, _clock;
		private bool _ready;
		public FoodReplica Replica { get; } = new();
		public ulong Epoch { get; private set; }
		public int SentPackets { get; private set; }
		public long SentBytes { get; private set; }

		public FoodReplication(FoodSpawner food, NetworkManager manager)
		{
			_food = food;
			_manager = manager;
			Epoch = (ulong)DateTime.UtcNow.Ticks;
			manager.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, Receive);
			manager.OnClientDisconnectCallback += Disconnected;
			if (food.World.Authoritative) { food.World.Changed += Changed; food.Eaten += QueueEffect; }
			Replica.Eaten += FoodSpawner.ShowEffect;
		}

		public void StartEpoch(ulong epoch)
		{
			Epoch = Math.Max(Epoch, epoch);
			_revision = 0;

			foreach (ulong client in _manager.ConnectedClientsIds)
			{
				if (_sentSnapshots.ContainsKey(client))
				{
					SendSnapshot(client);
				}
			}
		}

		public void MarkReady()
		{
			_ready = true;
			foreach (int slot in _dirty) _marked[slot] = false;
			_dirty.Clear();
		}
		private void Changed(FoodRecord food)
		{
			int slot = FoodWorld.SlotOf(food.Id);
			if (_marked[slot]) return;
			_marked[slot] = true;
			_dirty.Add(slot);
		}
		private void QueueEffect(FoodEatEvent effect) => _effects.Add(effect);
		private void Disconnected(ulong client) => _sentSnapshots.Remove(client);

		public void Tick(float deltaTime)
		{
			if (!_manager.IsListening) return;
			_clock += deltaTime;
			if (!_manager.IsServer)
			{
				_request -= deltaTime;
				if (Replica.NeedsSnapshot && _request <= 0f && _manager.IsConnectedClient)
				{
					_request = SnapshotRetrySeconds;
					_writer.Reset(FoodMessage.Request, 0, 0);
					Send(NetworkManager.ServerClientId);
				}
				return;
			}
			if (!_ready) return;
			_flush += deltaTime;
			if (_flush < 0.1f) return;
			_flush %= 0.1f;
			for (int start = 0; start < _dirty.Count; start += FoodPacketWriter.RecordsPerPacket)
			{
				int count = Math.Min(FoodPacketWriter.RecordsPerPacket, _dirty.Count - start);
				_writer.Reset(FoodMessage.Delta, Epoch, ++_revision);
				_writer.Byte((byte)count);
				for (int i = 0; i < count; i++)
				{
					int slot = _dirty[start + i];
					_writer.Record(_food.World.At(slot));
					_marked[slot] = false;
				}
				Broadcast();
			}
			_dirty.Clear();
			for (int start = 0; start < _effects.Count; start += FoodPacketWriter.RecordsPerPacket)
			{
				int count = Math.Min(FoodPacketWriter.RecordsPerPacket, _effects.Count - start);
				_writer.Reset(FoodMessage.Effects, Epoch, ++_revision);
				_writer.Byte((byte)count);
				for (int i = 0; i < count; i++)
				{
					FoodEatEvent effect = _effects[start + i];
					_writer.Float(effect.Position.x); _writer.Float(effect.Position.y); _writer.Float(effect.Size);
					_writer.Byte(effect.Flavor); _writer.Byte(effect.Golden ? (byte)1 : (byte)0);
				}
				Broadcast();
			}
			_effects.Clear();
		}

		private void Receive(ulong sender, FastBufferReader reader)
		{
			int length = reader.Length - reader.Position;
			if (length < 18 || length > _receive.Length) return;
			reader.ReadBytesSafe(ref _receive, length);
			if (_manager.IsServer)
			{
				if (!_ready || !_manager.ConnectedClients.ContainsKey(sender) || sender == _manager.LocalClientId ||
					length != 18 || _receive[0] != 1 || _receive[1] != (byte)FoodMessage.Request) return;
				if (!_sentSnapshots.TryGetValue(sender, out float time) || _clock - time >= 1f) SendSnapshot(sender);
			}
			else if (sender == NetworkManager.ServerClientId)
			{
				uint progress = Replica.SnapshotProgress;
				Replica.Receive(_receive, length);

				if (Replica.SnapshotProgress != progress)
				{
					_request = SnapshotRetrySeconds;
				}
			}
		}

		private void SendSnapshot(ulong client)
		{
			_sentSnapshots[client] = _clock;
			ulong token = ++_snapshot;
			_writer.Reset(FoodMessage.Begin, Epoch, _revision);
			_writer.UInt64(token); _writer.UInt32((uint)_food.World.ActiveCount);
			Send(client);
			uint hash = 2166136261;
			int sent = 0, slot = 0;
			while (slot < _food.World.UsedSlots)
			{
				_writer.Reset(FoodMessage.Chunk, Epoch, _revision);
				_writer.UInt64(token); _writer.UInt32((uint)sent);
				int countPosition = _writer.Length;
				_writer.Byte(0);
				int start = _writer.Length, count = 0;
				while (slot < _food.World.UsedSlots && count < FoodPacketWriter.RecordsPerPacket)
				{
					FoodRecord food = _food.World.At(slot++);
					if (food?.Active != true) continue;
					_writer.Record(food); count++;
				}
				if (count == 0) continue;
				_writer.Buffer[countPosition] = (byte)count;
				hash = FoodPacketWriter.Hash(hash, _writer.Buffer, start, _writer.Length - start);
				sent += count;
				Send(client);
			}
			_writer.Reset(FoodMessage.End, Epoch, _revision);
			_writer.UInt64(token); _writer.UInt32(hash);
			Send(client);
		}
		private void Broadcast()
		{
			var clients = _manager.ConnectedClientsIds;
			for (int i = 0; i < clients.Count; i++)
				if (clients[i] != _manager.LocalClientId && _sentSnapshots.ContainsKey(clients[i])) Send(clients[i]);
		}
		private void Send(ulong client)
		{
			using var writer = new FastBufferWriter(_writer.Length, Allocator.Temp);
			writer.WriteBytesSafe(_writer.Buffer, _writer.Length);
			_manager.CustomMessagingManager.SendNamedMessage(MessageName, client, writer, NetworkDelivery.ReliableSequenced);
			SentPackets++; SentBytes += _writer.Length;
		}
		public void Dispose()
		{
			_manager.OnClientDisconnectCallback -= Disconnected;
			_manager.CustomMessagingManager?.UnregisterNamedMessageHandler(MessageName);
			_food.World.Changed -= Changed; _food.Eaten -= QueueEffect;
			Replica.Eaten -= FoodSpawner.ShowEffect;
		}
	}
}
