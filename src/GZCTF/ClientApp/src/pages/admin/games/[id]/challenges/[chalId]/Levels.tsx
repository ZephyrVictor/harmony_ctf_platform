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
import { FC, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithChallengeEdit } from '@Components/admin/WithChallengeEdit'
import { COMMIT_TRAP_BAN_ANSWERS } from '@Utils/ChallengeLevelDefaults'
import { showErrorMsg } from '@Utils/Shared'
import api, {
  ChallengeLevelConfig,
  LevelConfigCreateModel,
  LevelConfigUpdateModel,
} from '@Api'

const createLevelEditModel = (config?: ChallengeLevelConfig): LevelConfigCreateModel =>
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

const LevelEditModal: FC<{
  opened: boolean
  onClose: () => void
  challengeId: number
  config?: ChallengeLevelConfig
  onSaved: () => void
}> = ({ opened, onClose, challengeId, config, onSaved }) => {
  const { t } = useTranslation()
  const [disabled, setDisabled] = useState(false)
  const [model, setModel] = useState<LevelConfigCreateModel>(() => createLevelEditModel(config))
  const trapEnabled = (model.banOnAnswers ?? []).length > 0
  const splitAnswers = (value: string) => value.split(',').map((s) => s.trim()).filter(Boolean)
  const wasOpen = useRef(false)

  // Only (re)initialize the form when the modal transitions closed -> open.
  // Reacting to `config` identity would let a background SWR revalidation
  // replace in-progress edits (e.g. Trap answers) mid-interaction.
  useEffect(() => {
    if (opened && !wasOpen.current) {
      setModel(createLevelEditModel(config))
      setDisabled(false)
    }
    wasOpen.current = opened
  }, [opened, config])

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
    <Modal
      opened={opened}
      onClose={onClose}
      title={config ? t('admin.level.edit') : t('admin.level.create')}
      size="lg"
    >
      <Stack gap="sm">
        <Group grow>
          <NumberInput
            label={t('admin.level.level')}
            value={model.level}
            onChange={(v) => setModel({ ...model, level: typeof v === 'number' ? v : 1 })}
            disabled={!!config}
          />
          <TextInput
            label={t('admin.level.levelTitle')}
            value={model.title ?? ''}
            onChange={(e) => setModel({ ...model, title: e.target.value })}
          />
        </Group>
        <Group grow>
          <NumberInput
            label={t('admin.level.score')}
            description={t('admin.level.scoreDescription')}
            value={model.score ?? 0}
            onChange={(v) => setModel({ ...model, score: typeof v === 'number' ? v : 0 })}
          />
          <NumberInput
            label={t('admin.level.penaltyScore')}
            description={t('admin.level.penaltyDescription')}
            value={model.penaltyScore ?? 0}
            onChange={(v) => setModel({ ...model, penaltyScore: typeof v === 'number' ? v : 0 })}
          />
        </Group>
        <TextInput
          label={t('admin.level.lockOnAnswers')}
          description={t('admin.level.lockOnAnswersDescription')}
          value={(model.lockOnAnswers ?? []).join(',')}
          onChange={(e) =>
            setModel({
              ...model,
              lockOnAnswers: splitAnswers(e.target.value),
            })
          }
        />
        <Card withBorder shadow="none" padding="sm">
          <Stack gap="xs">
            <Group justify="space-between" align="flex-start" wrap="nowrap">
              <Stack gap={2}>
                <Text fw={600} size="sm">
                  {t('admin.level.trap.title')}
                </Text>
                <Text c="dimmed" size="xs">
                  {t('admin.level.trap.description')}
                </Text>
              </Stack>
              <Switch
                label={t('admin.level.trap.enabled')}
                checked={trapEnabled}
                onChange={(e) => setModel((m) => ({
                  ...m,
                  banOnAnswers: e.target.checked ? [...COMMIT_TRAP_BAN_ANSWERS] : [],
                }))}
              />
            </Group>
            {trapEnabled && (
              <>
                <TextInput
                  label={t('admin.level.banOnAnswers')}
                  description={t('admin.level.banOnAnswersDescription')}
                  value={(model.banOnAnswers ?? []).join(',')}
                  onChange={(e) => setModel((m) => ({ ...m, banOnAnswers: splitAnswers(e.target.value) }))}
                />
                <Group justify="flex-end">
                  <Button
                    variant="light"
                    size="xs"
                    onClick={() => setModel((m) => ({ ...m, banOnAnswers: [...COMMIT_TRAP_BAN_ANSWERS] }))}
                  >
                    {t('admin.level.trap.restoreDefault')}
                  </Button>
                </Group>
              </>
            )}
          </Stack>
        </Card>
        <Group grow>
          <TextInput
            label={t('admin.import.level.orSeparators', { defaultValue: 'OR separators' })}
            description={t('admin.import.level.orSeparatorsDescription', {
              defaultValue: 'Separates Exact alternatives or SourceLocation line candidates',
            })}
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
          label={t('admin.level.isEnabled')}
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
          <Title>{t('admin.level.title')}</Title>
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
                            {t('admin.level.enabled')}
                          </Badge>
                        ) : (
                          <Badge variant="light" color="gray">
                            {t('admin.level.disabled')}
                          </Badge>
                        )}
                      </Group>
                      <Group gap="xs">
                        <Badge variant="outline">{t('admin.level.score')}: {cfg.score}</Badge>
                        <Badge variant="outline">{t('admin.level.penaltyScore')}: {cfg.penaltyScore}</Badge>
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
                          {t('admin.level.lockOnAnswers')}: {cfg.lockOnAnswers.join(', ')}
                        </Text>
                      )}
                      {cfg.banOnAnswers && cfg.banOnAnswers.length > 0 && (
                        <Text size="xs" c="red">
                          {t('admin.level.banOnAnswers')}: {cfg.banOnAnswers.join(', ')}
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
