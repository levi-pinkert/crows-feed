using UnityEngine.Audio;
using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayingSound
{
	public enum State
	{
		Paused,
		Playing,
		Stopped
	}

	public State state;
	public SoundInfo sound;

	private int channel;
	private AudioSource audioSource;
	private float volume = 1f;
	private bool looping;

	public PlayingSound(int channel, AudioSource audioSource, SoundInfo sound)
	{
		this.channel = channel;
		this.audioSource = audioSource;
		this.sound = sound;
		this.volume = 1f;
		audioSource.clip = sound.clip;
		audioSource.volume = sound.volume * this.volume;
		state = State.Paused;
	}

	public void SetPlaying(bool val)
	{
		if (state == State.Paused && val == true)
		{
			//unpause
			audioSource.Play();
			state = State.Playing;
		}
		else if (state == State.Playing && val == false)
		{
			//pause
			audioSource.Pause();
			state = State.Paused;
		}
	}

	public void Stop()
	{
		if (state != State.Stopped)
		{
			if (audioSource != null)
			{
				audioSource.Stop();
			}
			state = State.Stopped;
		}
	}

	public void Sync(PlayingSound other)
	{
		if (other.state == State.Playing)
		{
			SetPlaying(true);
			audioSource.time = other.audioSource.time;
		}
		else if (other.state == State.Paused)
		{
			SetPlaying(false);
		}
		else if (other.state == State.Stopped)
		{
			Stop();
		}
	}

	public void SetVolume(float val)
	{
		if (state == State.Stopped) { return; }
		volume = val;
		audioSource.volume = volume * sound.volume;
	}

	public void SetLooping(bool val)
	{
		if (state == State.Stopped) { return; }
		looping = val;
		audioSource.loop = looping;
	}

	public void Update()
	{
		//check if we've come to our natural end
		if (state == State.Playing && !looping && !audioSource.isPlaying)
		{
			Stop();
		}
	}

	public float GetVolume()
	{
		return volume;
	}

	public void FadeVolumeTowards(float val, float d)
	{
		if (volume < val)
		{
			SetVolume(Mathf.Min(volume + d, val));
		}
		else if (volume > val)
		{
			SetVolume(Mathf.Max(volume - d, val));
		}
	}
}

[Serializable]
public class SoundInfo
{
	public string name;
	public AudioClip clip;
	[Range(0f, 1f)]
	public float volume;
}

public class SoundManager : MonoBehaviour
{
	class Channel
	{
		public AudioSource source;
		public PlayingSound sound;
		public int idx;
	}

	public static SoundManager instance;

	public List<SoundInfo> sounds;

	private List<Channel> channels = new List<Channel>();
	private Dictionary<string, SoundInfo> soundMap = new Dictionary<string, SoundInfo>();

	private void Awake()
	{
		//singleton code
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else
		{
			Destroy(gameObject);
			return;
		}

		//set up dictionary
		foreach (SoundInfo s in sounds)
		{
			soundMap[s.name] = s;
		}
	}

	private void Update()
	{
		//update all channels
		foreach (Channel c in channels)
		{
			if (c.sound != null)
			{
				c.sound.Update();
			}
		}

		//try to free any stopped channels
		for (int i = 0; i < channels.Count; i++)
		{
			Channel channel = channels[i];
			if (channel.sound != null)
			{
				if (channel.sound.state == PlayingSound.State.Stopped)
				{
					channel.sound = null;
					ResetAudioSource(channel.source);
				}
			}
		}
	}

	private void ResetAudioSource(AudioSource s)
	{
		s.volume = 1f;
		s.loop = false;
		s.clip = null;
		s.playOnAwake = false;
	}

	private Channel GetFreeChannel()
	{
		//Find a free channel
		for (int i = 0; i < channels.Count; i++)
		{
			if (channels[i].sound == null)
			{
				return channels[i];
			}
		}

		//Create a new channel
		Channel newChannel = new Channel();
		newChannel.source = gameObject.AddComponent<AudioSource>();
		ResetAudioSource(newChannel.source);
		newChannel.idx = channels.Count;
		channels.Add(newChannel);
		return newChannel;
	}

	public PlayingSound InitializeSound(string soundName)
	{
		Channel c = GetFreeChannel();
		SoundInfo soundInfo = soundMap[soundName];
		if (soundInfo == null)
		{
			return null;
		}
		PlayingSound playingSound = new PlayingSound(c.idx, c.source, soundInfo);
		c.sound = playingSound;
		return playingSound;
	}

	public PlayingSound PlaySound(string soundName)
	{
		PlayingSound s = InitializeSound(soundName);
		s.SetPlaying(true);
		return s;
	}

	public PlayingSound PlaySoundLooping(string soundName)
	{
		PlayingSound s = InitializeSound(soundName);
		s.SetLooping(true);
		s.SetPlaying(true);
		return s;
	}

	public PlayingSound FindPlayingSound(string soundName)
	{
		foreach (Channel c in channels)
		{
			if (c.sound != null && c.sound.sound.name == soundName && c.sound.state != PlayingSound.State.Stopped)
			{
				return c.sound;
			}
		}
		return null;
	}

	public bool SoundExists(string soundName)
	{
		return soundMap.ContainsKey(soundName);
	}
}
