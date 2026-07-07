import {
  Button,
  Group,
  Modal,
  Stack,
  TextInput,
  NumberInput,
  Text,
  Switch,
  Divider,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiPlus, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { showErrorMsg } from '@Utils/Shared'
import { createDefaultTrackFullLevels } from '@Utils/ChallengeLevelDefaults'
import api, {
  ChallengeFlagAnswerType,
  TrackFullCreateModel,
  TrackFullLevelModel,
  TrackCreateModel,
} from '@Api'

interface TrackFullCreateModalProps {
  opened: boolean
  onClose: () => void
  challengeId: number
  onSaved: () => void
}

export const TrackFullCreateModal: FC<TrackFullCreateModalProps> = ({
  opened,
  onClose,
  challengeId,
  onSaved,
}) => {
  const { t } = useTranslation()
  const [disabled, setDisabled] = useState(false)
  const [track, setTrack] = useState<TrackCreateModel>({
    title: '',
    flawNo: '',
    repository: '',
    module: '',
    filePath: '',
    category: '',
    referenceUrl: '',
    isEnabled: true,
    sortOrder: 0,
  })
  const [levels, setLevels] = useState<TrackFullLevelModel[]>(createDefaultTrackFullLevels())

  const updateLevel = (idx: number, patch: Partial<TrackFullLevelModel>) => {
    setLevels((ls) => ls.map((l, i) => (i === idx ? { ...l, ...patch } : l)))
  }

  const addLevel = () => {
    setLevels((ls) => [
      ...ls,
      { level: ls.length + 1, flagValue: '', answerType: ChallengeFlagAnswerType.Exact, score: 0 },
    ])
  }

  const removeLevel = (idx: number) => {
    setLevels((ls) => ls.filter((_, i) => i !== idx).map((l, i) => ({ ...l, level: i + 1 })))
  }

  const onSave = async () => {
    if (levels.length === 0 || levels.some((l) => !l.flagValue.trim())) {
      showNotification({
        color: 'red',
        message: t('admin.notification.games.challenges.tracks.flags_required', {
          defaultValue: 'All levels require a flag value',
        }),
      })
      return
    }
    setDisabled(true)
    try {
      await api.track.trackCreateTrackFull(challengeId, {
        track,
        levels,
      })
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.tracks.created_full', {
          defaultValue: 'Track + flags created',
        }),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      onSaved()
      onClose()
      // reset
      setTrack({
        title: '',
        flawNo: '',
        repository: '',
        module: '',
        filePath: '',
        category: '',
        referenceUrl: '',
        isEnabled: true,
        sortOrder: 0,
      })
      setLevels(createDefaultTrackFullLevels())
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal opened={opened} onClose={onClose} title="Add Vulnerability (Track + Flags)" size="xl">
      <Stack gap="sm">
        <Text size="sm" c="dimmed">
          {t('admin.content.games.challenges.tracks.full_description', {
            defaultValue:
              'One call creates a Track and its L1/L2/L3 flags. L1 = file:line, L2 = commit hash, L3 = CWE.',
          })}
        </Text>
        <Divider label="Track metadata" />
        <Group grow>
          <TextInput
            label="Title"
            value={track.title ?? ''}
            onChange={(e) => setTrack({ ...track, title: e.target.value })}
          />
          <TextInput
            label="FlawNo"
            value={track.flawNo ?? ''}
            onChange={(e) => setTrack({ ...track, flawNo: e.target.value })}
          />
        </Group>
        <Group grow>
          <TextInput
            label="Repository"
            value={track.repository ?? ''}
            onChange={(e) => setTrack({ ...track, repository: e.target.value })}
          />
          <TextInput
            label="Module"
            value={track.module ?? ''}
            onChange={(e) => setTrack({ ...track, module: e.target.value })}
          />
        </Group>
        <TextInput
          label="FilePath"
          value={track.filePath ?? ''}
          onChange={(e) => setTrack({ ...track, filePath: e.target.value })}
        />
        <Group grow>
          <TextInput
            label="Category"
            value={track.category ?? ''}
            onChange={(e) => setTrack({ ...track, category: e.target.value })}
          />
          <TextInput
            label="ReferenceUrl"
            value={track.referenceUrl ?? ''}
            onChange={(e) => setTrack({ ...track, referenceUrl: e.target.value })}
          />
          <NumberInput
            label="SortOrder"
            value={track.sortOrder ?? 0}
            onChange={(v) => setTrack({ ...track, sortOrder: typeof v === 'number' ? v : 0 })}
          />
        </Group>
        <Switch
          label="IsEnabled"
          checked={track.isEnabled ?? true}
          onChange={(e) => setTrack({ ...track, isEnabled: e.target.checked })}
        />
        <Divider label="Levels (Flags)" />
        {levels.map((lvl, idx) => (
          <Stack key={idx} gap="xs">
            <Group grow align="flex-end">
              <NumberInput
                label={`Level ${idx + 1}`}
                value={lvl.level}
                onChange={(v) => updateLevel(idx, { level: typeof v === 'number' ? v : 1 })}
              />
              <TextInput
                label="FlagValue"
                placeholder="e.g. services/foo.cpp:c:56 / commit_hash / CWE-476"
                value={lvl.flagValue}
                onChange={(e) => updateLevel(idx, { flagValue: e.target.value })}
              />
              <NumberInput
                label="Score"
                value={lvl.score ?? 0}
                onChange={(v) => updateLevel(idx, { score: typeof v === 'number' ? v : 0 })}
              />
              <Button variant="light" color="red" onClick={() => removeLevel(idx)}>
                <Icon path={mdiDeleteOutline} size={1} />
              </Button>
            </Group>
          </Stack>
        ))}
        <Button variant="light" leftSection={<Icon path={mdiPlus} size={1} />} onClick={addLevel}>
          {t('admin.button.challenges.tracks.add_level', { defaultValue: 'Add Level' })}
        </Button>
        <Divider />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            {t('common.button.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button disabled={disabled} onClick={onSave}>
            {t('common.button.create', { defaultValue: 'Create' })}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
