import {
  ActionIcon, Badge, Button, Card, Checkbox, Group, Input, Paper,
  SimpleGrid, Stack, Text,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiDeleteOutline, mdiPencilOffOutline, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useCallback, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useDisplayInputStyles } from '@Utils/ThemeOverride'
import { Attachment, FlagInfoModel } from '@Api'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { batchDeleteFlags, batchUpdateFlags } from '@Utils/ExtApi'

interface FlagCardProps {
  flag: FlagInfoModel
  onDelete: () => void
  unifiedAttachment?: Attachment | null
  selected: boolean
  onToggleSelect: () => void
}

const FlagCard: FC<FlagCardProps> = ({ flag, onDelete, unifiedAttachment, selected, onToggleSelect }) => {
  const clipboard = useClipboard()
  const attachment = unifiedAttachment ?? flag.attachment
  const shortURL = attachment?.url?.split('/').slice(-2)[0].slice(0, 8)
  const { classes } = useDisplayInputStyles({ fw: 'bold', ff: 'monospace', cs: 'pointer' })
  const { t } = useTranslation()

  return (
    <Card p="sm" style={{ position: 'relative', outline: selected ? '2px solid var(--mantine-color-blue-5)' : undefined }}>
      <Group wrap="nowrap" justify="space-between" gap={3}>
        <Stack align="flex-start" gap={0} w="100%">
          <Input
            variant="unstyled"
            value={flag.flag}
            w="100%"
            size="md"
            readOnly
            onClick={() => {
              clipboard.copy(flag.flag)
              showNotification({
                message: t('admin.notification.games.challenges.flag.copied'),
                color: 'teal',
                icon: <Icon path={mdiCheck} size={1} />,
              })
            }}
            classNames={classes}
          />
          <Text c="dimmed" size="sm" ff="monospace">
            {attachment?.type} {shortURL}
          </Text>
        </Stack>
        <Group gap={4}>
          <Checkbox
            checked={selected}
            onChange={onToggleSelect}
            aria-label={t('admin.flag.select_flag', { defaultValue: 'Select flag' })}
          />
          <ActionIcon onClick={onDelete} color="red">
            <Icon path={mdiDeleteOutline} size={1} />
          </ActionIcon>
        </Group>
      </Group>
    </Card>
  )
}

interface FlagEditPanelProps {
  flags?: FlagInfoModel[]
  onDelete: (flag: FlagInfoModel) => void
  unifiedAttachment?: Attachment | null
  challengeId?: number
  onRefresh?: () => void
}

export const FlagEditPanel: FC<FlagEditPanelProps> = ({ flags, onDelete, unifiedAttachment, challengeId, onRefresh }) => {
  const { t } = useTranslation()
  const [selected, setSelected] = useState<Set<string>>(new Set())

  const toggle = useCallback((id: string) => {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }, [])

  const clearSelection = useCallback(() => setSelected(new Set()), [])

  const selectedIds = useCallback(() => {
    return (flags ?? [])
      .filter((f) => f.id && selected.has(f.id))
      .map((f) => f.id!) // eslint-disable-line @typescript-eslint/no-non-null-assertion
  }, [flags, selected])

  const handleBatchUpdate = async (isEnabled: boolean) => {
    if (!challengeId) return
    const ids = selectedIds()
    if (ids.length === 0) return
    try {
      await batchUpdateFlags(challengeId, { flagIds: ids, isEnabled })
      showSuccessMsg(
        t(isEnabled ? 'admin.flag.batch_enable_success' : 'admin.flag.batch_disable_success',
          { count: ids.length }),
        t
      )
      clearSelection()
      onRefresh?.()
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const handleBatchDelete = async () => {
    if (!challengeId) return
    const ids = selectedIds()
    if (ids.length === 0) return
    if (!window.confirm(
      t('admin.flag.batch_delete_confirm', { count: ids.length })
    )) return
    try {
      await batchDeleteFlags(challengeId, { flagIds: ids })
      showSuccessMsg(t('admin.flag.batch_delete_success', { count: ids.length }), t)
      clearSelection()
      onRefresh?.()
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const allSelected = flags && flags.length > 0 && flags.every((f) => f.id && selected.has(f.id))
  const toggleAll = () => {
    if (!flags) return
    if (allSelected) {
      clearSelection()
    } else {
      setSelected(new Set(flags.map((f) => f.id).filter(Boolean) as string[]))
    }
  }

  return (
    <Stack>
      {selected.size > 0 && (
        <Paper withBorder p="sm" shadow="xs">
          <Group justify="space-between">
            <Group gap="sm">
              <Checkbox
                checked={!!allSelected}
                onChange={toggleAll}
                aria-label={t('admin.flag.select_all', { defaultValue: 'Select all' })}
              />
              <Badge color="blue" variant="light">
                {t('admin.flag.selected_count', { count: selected.size })}
              </Badge>
            </Group>
            <Group gap="xs">
              <Button size="xs" variant="light" color="green"
                      leftSection={<Icon path={mdiPencilOutline} size={0.7} />}
                      onClick={() => handleBatchUpdate(true)}>
                {t('admin.flag.batch_enable')}
              </Button>
              <Button size="xs" variant="light" color="orange"
                      leftSection={<Icon path={mdiPencilOffOutline} size={0.7} />}
                      onClick={() => handleBatchUpdate(false)}>
                {t('admin.flag.batch_disable')}
              </Button>
              <Button size="xs" color="red" variant="light"
                      leftSection={<Icon path={mdiDeleteOutline} size={0.7} />}
                      onClick={handleBatchDelete}>
                {t('admin.flag.batch_delete')}
              </Button>
              <ActionIcon onClick={clearSelection} title={t('admin.flag.clear_selection', { defaultValue: 'Clear' })}>
                <Icon path={mdiClose} size={1} />
              </ActionIcon>
            </Group>
          </Group>
        </Paper>
      )}
      <SimpleGrid spacing="sm" cols={{ base: 2, w18: 3, w24: 4, w30: 5, w36: 6, w42: 7, w48: 8 }}>
        {flags &&
          flags.map((flag, i) => (
            <FlagCard
              key={flag.id ?? i}
              flag={flag}
              onDelete={() => onDelete(flag)}
              unifiedAttachment={unifiedAttachment}
              selected={!!(flag.id && selected.has(flag.id))}
              onToggleSelect={() => flag.id && toggle(flag.id)}
            />
          ))}
      </SimpleGrid>
    </Stack>
  )
}
