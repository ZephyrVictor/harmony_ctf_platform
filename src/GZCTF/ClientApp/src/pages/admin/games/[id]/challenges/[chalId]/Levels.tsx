import {
  ActionIcon,
  Badge,
  Button,
  Card,
  Center,
  Group,
  Modal,
  NumberInput,
  ScrollArea,
  Stack,
  Switch,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline, mdiPlus, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithChallengeEdit } from '@Components/admin/WithChallengeEdit'
import { showErrorMsg } from '@Utils/Shared'
import api, {
  ChallengeLevelConfig,
  LevelConfigCreateModel,
  LevelConfigUpdateModel,
} from '@Api'

const LevelEditModal: FC<{
  opened: boolean
  onClose: () => void
  challengeId: number
  config?: ChallengeLevelConfig
  onSaved: () => void
}> = ({ opened, onClose, challengeId, config, onSaved }) => {
  const { t } = useTranslation()
  const [disabled, setDisabled] = useState(false)
  const [model, setModel] = useState<LevelConfigCreateModel>(
    config
      ? {
          level: config.level,
          title: config.title,
          score: config.score,
          penaltyScore: config.penaltyScore,
          lockOnAnswers: config.lockOnAnswers ?? [],
          banOnAnswers: config.banOnAnswers ?? [],
          caseSensitive: config.caseSensitive ?? true,
          pathCaseSensitive: config.pathCaseSensitive ?? true,
          orSeparators: config.orSeparators ?? '、,;',
          rangeSeparator: config.rangeSeparator ?? '-',
          stripLinePrefix: config.stripLinePrefix ?? true,
          isEnabled: config.isEnabled,
          sortOrder: config.sortOrder,
        }
      : {
          level: 1,
          title: '',
          score: 5,
          penaltyScore: 0,
          lockOnAnswers: [],
          banOnAnswers: [],
          caseSensitive: true,
          pathCaseSensitive: true,
          orSeparators: '、,;',
          rangeSeparator: '-',
          stripLinePrefix: true,
          isEnabled: true,
          sortOrder: 1,
        }
  )

  const onSave = async () => {
    setDisabled(true)
    try {
      if (config) {
        await api.levelConfig.levelConfigUpdateLevel(challengeId, config.id!, model as LevelConfigUpdateModel)
      } else {
        await api.levelConfig.levelConfigCreateLevel(challengeId, model)
      }
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.levels.saved', { defaultValue: 'Level config saved' }),
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
    <Modal opened={opened} onClose={onClose} title={config ? 'Edit Level' : 'Create Level'} size="lg">
      <Stack gap="sm">
        <Group grow>
          <NumberInput
            label="Level"
            value={model.level}
            onChange={(v) => setModel({ ...model, level: typeof v === 'number' ? v : 1 })}
            disabled={!!config}
          />
          <TextInput
            label="Title"
            value={model.title ?? ''}
            onChange={(e) => setModel({ ...model, title: e.target.value })}
          />
        </Group>
        <Group grow>
          <NumberInput
            label="Score (default: L1=5 / L2=4 / L3=1)"
            value={model.score ?? 0}
            onChange={(v) => setModel({ ...model, score: typeof v === 'number' ? v : 0 })}
          />
          <NumberInput
            label="PenaltyScore"
            value={model.penaltyScore ?? 0}
            onChange={(v) => setModel({ ...model, penaltyScore: typeof v === 'number' ? v : 0 })}
          />
        </Group>
        <TextInput
          label="LockOnAnswers (comma-separated)"
          value={(model.lockOnAnswers ?? []).join(',')}
          onChange={(e) =>
            setModel({
              ...model,
              lockOnAnswers: e.target.value.split(',').map((s) => s.trim()).filter(Boolean),
            })
          }
        />
        <TextInput
          label="BanOnAnswers (comma-separated)"
          value={(model.banOnAnswers ?? []).join(',')}
          onChange={(e) =>
            setModel({
              ...model,
              banOnAnswers: e.target.value.split(',').map((s) => s.trim()).filter(Boolean),
            })
          }
        />
        <Group grow>
          <TextInput
            label={t('admin.import.level.orSeparators', { defaultValue: 'OR separators' })}
            value={model.orSeparators ?? '、,;'}
            onChange={(e) => setModel({ ...model, orSeparators: e.target.value })}
          />
          <TextInput
            label={t('admin.import.level.rangeSeparator', { defaultValue: 'Range separator' })}
            value={model.rangeSeparator ?? '-'}
            onChange={(e) => setModel({ ...model, rangeSeparator: e.target.value })}
          />
        </Group>
        <Group>
          <Switch
            label={t('admin.import.level.caseSensitive', { defaultValue: 'Case sensitive' })}
            checked={model.caseSensitive ?? true}
            onChange={(e) => setModel({ ...model, caseSensitive: e.target.checked })}
          />
          <Switch
            label={t('admin.import.level.pathCaseSensitive', { defaultValue: 'Path case sensitive' })}
            checked={model.pathCaseSensitive ?? true}
            onChange={(e) => setModel({ ...model, pathCaseSensitive: e.target.checked })}
          />
          <Switch
            label={t('admin.import.level.stripLinePrefix', { defaultValue: 'Strip c: prefix' })}
            checked={model.stripLinePrefix ?? true}
            onChange={(e) => setModel({ ...model, stripLinePrefix: e.target.checked })}
          />
        </Group>
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

const AdminLevels: FC = () => {
  const { id, chalId } = useParams()
  const [numId, numCId] = [parseInt(id ?? '-1'), parseInt(chalId ?? '-1')]
  const { t } = useTranslation()

  const { data: levels, mutate } = api.levelConfig.useLevelConfigGetLevels(numCId)
  const [editOpened, setEditOpened] = useState(false)
  const [editing, setEditing] = useState<ChallengeLevelConfig | undefined>(undefined)

  const onDelete = async (levelId: number | undefined) => {
    if (levelId === undefined) return
    try {
      await api.levelConfig.levelConfigDeleteLevel(numCId, levelId)
      showNotification({
        color: 'teal',
        message: t('admin.notification.games.challenges.levels.deleted', { defaultValue: 'Level config deleted' }),
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
          <Title>Level Configs</Title>
          <Button
            leftSection={<Icon path={mdiPlus} size={1} />}
            onClick={() => {
              setEditing(undefined)
              setEditOpened(true)
            }}
          >
            {t('admin.button.challenges.levels.add', { defaultValue: 'Add Level' })}
          </Button>
        </>
      }
    >
      <Stack>
        {levels && levels.length === 0 ? (
          <Center h="40vh">
            <Title order={3} c="dimmed">
              {t('admin.content.games.challenges.levels.empty', { defaultValue: 'No level configs yet' })}
            </Title>
          </Center>
        ) : (
          <ScrollArea h="calc(100vh - 12rem)">
            <Stack gap="sm">
              {levels?.map((cfg) => (
                <Card key={cfg.id} shadow="xs" padding="md" withBorder>
                  <Group justify="space-between" align="flex-start">
                    <Stack gap={4}>
                      <Group gap="xs">
                        <Badge variant="filled" color="indigo">
                          L{cfg.level}
                        </Badge>
                        <Text fw={700}>{cfg.title || '(no title)'}</Text>
                        {cfg.isEnabled ? (
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
                        <Badge variant="outline">Score: {cfg.score}</Badge>
                        <Badge variant="outline">Penalty: {cfg.penaltyScore}</Badge>
                        <Badge variant="outline">
                          {cfg.caseSensitive
                            ? t('admin.import.level.caseSensitiveBadge')
                            : t('admin.import.level.caseInsensitiveBadge')}
                        </Badge>
                        <Badge variant="outline">
                          OR: {cfg.orSeparators || '、,;'}
                        </Badge>
                        <Badge variant="outline">
                          Range: {cfg.rangeSeparator || '-'}
                        </Badge>
                      </Group>
                      {cfg.lockOnAnswers && cfg.lockOnAnswers.length > 0 && (
                        <Text size="xs" c="orange">
                          LockOn: {cfg.lockOnAnswers.join(', ')}
                        </Text>
                      )}
                      {cfg.banOnAnswers && cfg.banOnAnswers.length > 0 && (
                        <Text size="xs" c="red">
                          BanOn: {cfg.banOnAnswers.join(', ')}
                        </Text>
                      )}
                    </Stack>
                    <Group>
                      <ActionIcon
                        variant="light"
                        onClick={() => {
                          setEditing(cfg)
                          setEditOpened(true)
                        }}
                      >
                        <Icon path={mdiPencilOutline} size={1} />
                      </ActionIcon>
                      <ActionIcon variant="light" color="red" onClick={() => onDelete(cfg.id)}>
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
      <LevelEditModal
        opened={editOpened}
        onClose={() => setEditOpened(false)}
        challengeId={numCId}
        config={editing}
        onSaved={() => mutate()}
      />
    </WithChallengeEdit>
  )
}

export default AdminLevels
