import {
  Alert,
  Badge,
  Button,
  Card,
  Center,
  Group,
  Modal,
  ScrollArea,
  Select,
  Stack,
  Text,
  Textarea,
  TextInput,
  Title,
  NumberInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiPlus, mdiBugCheckOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithNavBar } from '@Components/WithNavbar'
import { showErrorMsg } from '@Utils/Shared'
import api, { ZeroDayTicketDto, ZeroDayTicketStatus, ZeroDaySeverity } from '@Api'

const statusColor: Record<ZeroDayTicketStatus, string> = {
  [ZeroDayTicketStatus.Pending]: 'yellow',
  [ZeroDayTicketStatus.Approved]: 'teal',
  [ZeroDayTicketStatus.Rejected]: 'red',
}

const statusLabel: Record<ZeroDayTicketStatus, string> = {
  [ZeroDayTicketStatus.Pending]: 'game.content.zeroday.status.pending',
  [ZeroDayTicketStatus.Approved]: 'game.content.zeroday.status.approved',
  [ZeroDayTicketStatus.Rejected]: 'game.content.zeroday.status.rejected',
}

const ZeroDayTicketSubmitModal: FC<{
  opened: boolean
  onClose: () => void
  gameId: number
  onSubmitted: () => void
}> = ({ opened, onClose, gameId, onSubmitted }) => {
  const { t } = useTranslation()
  const [disabled, setDisabled] = useState(false)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [challengeId, setChallengeId] = useState<number | undefined>(undefined)
  const [severity, setSeverity] = useState<ZeroDaySeverity>(ZeroDaySeverity.None)
  const [mod, setMod] = useState('')
  const [filePath, setFilePath] = useState('')
  const [lineNumber, setLineNumber] = useState('')
  const [commitHash, setCommitHash] = useState('')
  const [cwe, setCwe] = useState('')
  const [poc, setPoc] = useState('')

  const onSubmit = async () => {
    if (!description.trim()) {
      showNotification({
        color: 'red',
        message: t('game.notification.zeroday.description_required', {
          defaultValue: 'Description is required',
        }),
      })
      return
    }
    setDisabled(true)
    try {
      await api.zeroDayTicket.zeroDayTicketCreate({
        title: title || undefined,
        description,
        gameId,
        challengeId: challengeId || undefined,
        severity,
        module: mod || undefined,
        filePath: filePath || undefined,
        lineNumber: lineNumber || undefined,
        commitHash: commitHash || undefined,
        cwe: cwe || undefined,
        poC: poc || undefined,
      })
      showNotification({
        color: 'teal',
        message: t('game.notification.zeroday.submitted', { defaultValue: '0day ticket submitted' }),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      setTitle('')
      setDescription('')
      setChallengeId(undefined)
      setSeverity(ZeroDaySeverity.None)
      setMod('')
      setFilePath('')
      setLineNumber('')
      setCommitHash('')
      setCwe('')
      setPoc('')
      onSubmitted()
      onClose()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <Modal opened={opened} onClose={onClose} title={t('game.content.zeroday.modal.title')} size="lg">
      <Stack gap="sm">
        <TextInput
          label={t('game.content.zeroday.modal.title_field')}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <Textarea
          label={t('game.content.zeroday.modal.description')}
          placeholder={t('game.content.zeroday.modal.description_placeholder')}
          value={description}
          autosize
          minRows={4}
          onChange={(e) => setDescription(e.target.value)}
        />
        <NumberInput
          label={t('game.content.zeroday.modal.challenge_id')}
          value={challengeId}
          onChange={(v) => setChallengeId(typeof v === 'number' ? v : undefined)}
        />
        <Select
          label={t('game.content.zeroday.modal.severity')}
          value={severity}
          onChange={(v) => setSeverity((v as ZeroDaySeverity) ?? ZeroDaySeverity.None)}
          data={[
            { value: ZeroDaySeverity.None, label: t('game.content.zeroday.severity.none') },
            { value: ZeroDaySeverity.Low, label: t('game.content.zeroday.severity.low') },
            { value: ZeroDaySeverity.Medium, label: t('game.content.zeroday.severity.medium') },
            { value: ZeroDaySeverity.High, label: t('game.content.zeroday.severity.high') },
            { value: ZeroDaySeverity.Critical, label: t('game.content.zeroday.severity.critical') },
          ]}
        />
        <TextInput
          label={t('game.content.zeroday.modal.module')}
          value={mod}
          onChange={(e) => setMod(e.target.value)}
        />
        <TextInput
          label={t('game.content.zeroday.modal.file_path')}
          value={filePath}
          onChange={(e) => setFilePath(e.target.value)}
        />
        <TextInput
          label={t('game.content.zeroday.modal.line_number')}
          value={lineNumber}
          onChange={(e) => setLineNumber(e.target.value)}
        />
        <TextInput
          label={t('game.content.zeroday.modal.commit_hash')}
          value={commitHash}
          onChange={(e) => setCommitHash(e.target.value)}
        />
        <TextInput
          label={t('game.content.zeroday.modal.cwe')}
          value={cwe}
          onChange={(e) => setCwe(e.target.value)}
        />
        <Textarea
          label={t('game.content.zeroday.modal.poc')}
          value={poc}
          autosize
          minRows={2}
          onChange={(e) => setPoc(e.target.value)}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            {t('common.button.cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button disabled={disabled} onClick={onSubmit}>
            {t('common.button.submit', { defaultValue: 'Submit' })}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

const ZeroDayTickets: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { t } = useTranslation()

  const { data: tickets, mutate } = api.zeroDayTicket.useZeroDayTicketGetMyTickets({ gameId: numId })
  const { data: detail } = api.game.useGameChallengesWithTeamInfo(numId)
  const [submitOpened, setSubmitOpened] = useState(false)
  const ndayScore = detail?.ndayScore ?? 0
  const zeroDayGate = detail?.zeroDayNdayScoreRequirement ?? 200
  const canSubmitZeroDay = detail?.canSubmitZeroDay ?? (zeroDayGate <= 0 || ndayScore >= zeroDayGate)

  return (
    <WithNavBar width="100%" isLoading={!tickets} minWidth={0}>
      <Stack p="md" gap="md">
        <Group justify="space-between">
          <Group>
            <Icon path={mdiBugCheckOutline} size={2} />
            <Title>{t('game.content.zeroday.title')}</Title>
          </Group>
          <Button
            leftSection={<Icon path={mdiPlus} size={1} />}
            onClick={() => setSubmitOpened(true)}
            disabled={!canSubmitZeroDay}
          >
            {t('game.button.zeroday.submit')}
          </Button>
        </Group>
        {!canSubmitZeroDay && (
          <Alert color="yellow">
            {t('game.content.zeroday.gate', {
              current: ndayScore,
              required: zeroDayGate,
              defaultValue: `0day submission requires ${zeroDayGate} Nday points. Current Nday score: ${ndayScore}.`,
            })}
          </Alert>
        )}
        {tickets && tickets.length === 0 ? (
          <Center h="40vh">
            <Stack align="center">
              <Title order={3} c="dimmed">
                {t('game.content.zeroday.empty')}
              </Title>
              <Text c="dimmed">
                {t('game.content.zeroday.empty_hint')}
              </Text>
            </Stack>
          </Center>
        ) : (
          <ScrollArea h="calc(100vh - 12rem)">
            <Stack gap="sm">
              {tickets?.map((ticket) => (
                <Card key={ticket.id} shadow="xs" padding="md" withBorder>
                  <Group justify="space-between" align="flex-start">
                    <Stack gap={4}>
                      <Group gap="xs">
                        <Text fw={700}>{ticket.title || t('game.content.zeroday.no_title')}</Text>
                        <Badge variant="light" color={statusColor[ticket.status ?? ZeroDayTicketStatus.Pending]}>
                          {t(statusLabel[ticket.status ?? ZeroDayTicketStatus.Pending])}
                        </Badge>
                      </Group>
                      <Text size="sm" lineClamp={3}>
                        {ticket.description}
                      </Text>
                      {ticket.reviewComment && (
                        <Text size="sm" c="dimmed">
                          <strong>{t('game.content.zeroday.review')}:</strong> {ticket.reviewComment}
                        </Text>
                      )}
                      {ticket.bonusScore && ticket.bonusScore > 0 && (
                        <Badge variant="outline" color="teal">
                          {t('game.content.zeroday.bonus')}: +{ticket.bonusScore}
                        </Badge>
                      )}
                      <Text size="xs" c="dimmed">
                        {ticket.createdAt ? dayjs(ticket.createdAt).format('YYYY-MM-DD HH:mm') : ''}
                        {ticket.reviewedAt
                          ? ` | ${t('game.content.zeroday.reviewed')}: ${dayjs(ticket.reviewedAt).format('YYYY-MM-DD HH:mm')}`
                          : ''}
                      </Text>
                    </Stack>
                  </Group>
                </Card>
              ))}
            </Stack>
          </ScrollArea>
        )}
      </Stack>
      <ZeroDayTicketSubmitModal
        opened={submitOpened}
        onClose={() => setSubmitOpened(false)}
        gameId={numId}
        onSubmitted={() => mutate()}
      />
    </WithNavBar>
  )
}

export default ZeroDayTickets
