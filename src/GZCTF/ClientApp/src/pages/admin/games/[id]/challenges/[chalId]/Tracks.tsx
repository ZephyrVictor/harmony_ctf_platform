import {
  ActionIcon,
  Badge,
  Button,
  Card,
  Center,
  Group,
  Modal,
  ScrollArea,
  Stack,
  Text,
  TextInput,
  Textarea,
  Title,
  Switch,
  NumberInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline, mdiPlus, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithChallengeEdit } from '@Components/admin/WithChallengeEdit'
import { TrackFullCreateModal } from '@Components/admin/TrackFullCreateModal'
import { showErrorMsg } from '@Utils/Shared'
import api, { ChallengeTrack, TrackCreateModel, TrackUpdateModel } from '@Api'

const TrackEditModal: FC<{
  opened: boolean
  onClose: () => void
  challengeId: number
  track?: ChallengeTrack
  onSaved: () => void
}> = ({ opened, onClose, challengeId, track, onSaved }) => {
  const { t } = useTranslation()
  const [disabled, setDisabled] = useState(false)
  const [model, setModel] = useState<TrackUpdateModel>(
    track
      ? { ...track }
      : {
          title: '',
          flawNo: '',
          repository: '',
          module: '',
          filePath: '',
          category: '',
          referenceUrl: '',
          isEnabled: true,
          sortOrder: 0,
        }
  )

  const onSave = async () => {
    setDisabled(true)
    try {
      if (track) {
        await api.track.trackUpdateTrack(challengeId, track.id!, model)
      } else {
        await api.track.trackCreateTrack(challengeId, model as TrackCreateModel)
      }
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.tracks.saved', { defaultValue: 'Track saved' }),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      onSaved()
      onClose()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal opened={opened} onClose={onClose} title={track ? 'Edit Track' : 'Create Track'} size="lg">
      <Stack gap="sm">
        <TextInput
          label="Title"
          value={model.title ?? ''}
          onChange={(e) => setModel({ ...model, title: e.target.value })}
        />
        <TextInput
          label="FlawNo"
          value={model.flawNo ?? ''}
          onChange={(e) => setModel({ ...model, flawNo: e.target.value })}
        />
        <TextInput
          label="Repository"
          value={model.repository ?? ''}
          onChange={(e) => setModel({ ...model, repository: e.target.value })}
        />
        <TextInput
          label="Module"
          value={model.module ?? ''}
          onChange={(e) => setModel({ ...model, module: e.target.value })}
        />
        <TextInput
          label="FilePath"
          value={model.filePath ?? ''}
          onChange={(e) => setModel({ ...model, filePath: e.target.value })}
        />
        <TextInput
          label="Category"
          value={model.category ?? ''}
          onChange={(e) => setModel({ ...model, category: e.target.value })}
        />
        <TextInput
          label="ReferenceUrl"
          value={model.referenceUrl ?? ''}
          onChange={(e) => setModel({ ...model, referenceUrl: e.target.value })}
        />
        <NumberInput
          label="SortOrder"
          value={model.sortOrder ?? 0}
          onChange={(v) => setModel({ ...model, sortOrder: typeof v === 'number' ? v : 0 })}
        />
        <Switch
          label="IsEnabled"
          checked={model.isEnabled ?? true}
          onChange={(e) => setModel({ ...model, isEnabled: e.target.checked })}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            {t('common.button.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button disabled={disabled} onClick={onSave}>
            {t('common.button.save', { defaultValue: 'Save' })}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

const AdminTracks: FC = () => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const { t } = useTranslation()

  const { data: tracks, mutate } = api.track.useTrackGetTracks(numCId)
  const [editOpened, setEditOpened] = useState(false)
  const [editing, setEditing] = useState<ChallengeTrack | undefined>(undefined)
  const [fullCreateOpened, setFullCreateOpened] = useState(false)

  const onDelete = async (trackId: string | undefined) => {
    if (!trackId) return
    try {
      await api.track.trackDeleteTrack(numCId, trackId)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.tracks.deleted', { defaultValue: 'Track deleted' }),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  return (
    <WithChallengeEdit
      isLoading={false}
      backUrl={`/admin/games/${numId}/challenges/${numCId}`}
      head={
        <>
          <Title>Tracks</Title>
          <Group>
            <Button
              leftSection={<Icon path={mdiPlus} size={1} />}
              onClick={() => {
                setEditing(undefined)
                setEditOpened(true)
              }}
            >
              {t('admin.button.challenges.tracks.add', { defaultValue: 'Add Track' })}
            </Button>
            <Button
              color="green"
              leftSection={<Icon path={mdiPlus} size={1} />}
              onClick={() => setFullCreateOpened(true)}
            >
              {t('admin.button.challenges.tracks.add_full', { defaultValue: 'Add Vulnerability (Track + Flags)' })}
            </Button>
          </Group>
        </>
      }
    >
      <Stack>
        {tracks && tracks.length === 0 ? (
          <Center h="40vh">
            <Stack align="center">
              <Title order={3} c="dimmed">
                {t('admin.content.games.challenges.tracks.empty', { defaultValue: 'No tracks yet' })}
              </Title>
              <Text c="dimmed">
                {t('admin.content.games.challenges.tracks.empty_hint', {
                  defaultValue: 'Click "Add Vulnerability" to create a track with L1/L2/L3 flags in one call',
                })}
              </Text>
            </Stack>
          </Center>
        ) : (
          <ScrollArea h="calc(100vh - 12rem)">
            <Stack gap="sm">
              {tracks?.map((track) => (
                <Card key={track.id} shadow="xs" padding="md" withBorder>
                  <Group justify="space-between" align="flex-start">
                    <Stack gap={4}>
                      <Group gap="xs">
                        <Text fw={700}>{track.title || '(no title)'}</Text>
                        {track.isEnabled ? (
                          <Badge variant="light" color="teal">
                            enabled
                          </Badge>
                        ) : (
                          <Badge variant="light" color="gray">
                            disabled
                          </Badge>
                        )}
                      </Group>
                      <Group gap="xs">
                        {track.flawNo && <Badge variant="outline">FlawNo: {track.flawNo}</Badge>}
                        {track.repository && <Badge variant="outline">Repo: {track.repository}</Badge>}
                        {track.module && <Badge variant="outline">Module: {track.module}</Badge>}
                      </Group>
                      {track.filePath && (
                        <Text size="sm" ff="monospace">
                          {track.filePath}
                        </Text>
                      )}
                      {track.category && (
                        <Text size="xs" c="dimmed">
                          Category: {track.category}
                        </Text>
                      )}
                      {track.referenceUrl && (
                        <Text size="xs" c="dimmed">
                          Ref: {track.referenceUrl}
                        </Text>
                      )}
                    </Stack>
                    <Group>
                      <ActionIcon
                        variant="light"
                        onClick={() => {
                          setEditing(track)
                          setEditOpened(true)
                        }}
                      >
                        <Icon path={mdiPencilOutline} size={1} />
                      </ActionIcon>
                      <ActionIcon variant="light" color="red" onClick={() => onDelete(track.id)}>
                        <Icon path={mdiDeleteOutline} size={1} />
                      </ActionIcon>
                    </Group>
                  </Group>
                </Card>
              ))}
            </Stack>
          </ScrollArea>
        )}
      </Stack>
      <TrackEditModal
        opened={editOpened}
        onClose={() => setEditOpened(false)}
        challengeId={numCId}
        track={editing}
        onSaved={() => mutate()}
      />
      <TrackFullCreateModal
        opened={fullCreateOpened}
        onClose={() => setFullCreateOpened(false)}
        challengeId={numCId}
        onSaved={() => mutate()}
      />
    </WithChallengeEdit>
  )
}

export default AdminTracks
