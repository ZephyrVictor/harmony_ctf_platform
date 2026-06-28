import {
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
  [ZeroDayTicketStatus.Pending]: 'Pending',
  [ZeroDayTicketStatus.Approved]: 'Approved',
  [ZeroDayTicketStatus.Rejected]: 'Rejected',
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
    <Modal opened={opened} onClose={onClose} title="Submit 0day Ticket" size="lg">
      <Stack gap="sm">
        <TextInput
          label="Title (optional)"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
        <Textarea
          label="Description (required)"
          placeholder="Describe the vulnerability: where, how, impact"
          value={description}
          autosize
          minRows={4}
          onChange={(e) => setDescription(e.target.value)}
        />
        <NumberInput
          label="ChallengeId (optional - if tied to a specific challenge)"
          value={challengeId}
          onChange={(v) => setChallengeId(typeof v === 'number' ? v : undefined)}
        />
        <Select
          label="Severity (bonus tier: Critical N×100, High N×60, Medium N×30, Low N×10)"
          value={severity}
          onChange={(v) => setSeverity((v as ZeroDaySeverity) ?? ZeroDaySeverity.None)}
          data={[
            { value: ZeroDaySeverity.None, label: 'None' },
            { value: ZeroDaySeverity.Low, label: 'Low' },
            { value: ZeroDaySeverity.Medium, label: 'Medium' },
            { value: ZeroDaySeverity.High, label: 'High' },
            { value: ZeroDaySeverity.Critical, label: 'Critical' },
          ]}
        />
        <TextInput
          label="Module (e.g. ecmascript/base)"
          value={mod}
          onChange={(e) => setMod(e.target.value)}
        />
        <TextInput
          label="FilePath (L1 flag key, e.g. ecmascript/base/foo.cpp)"
          value={filePath}
          onChange={(e) => setFilePath(e.target.value)}
        />
        <TextInput
          label="LineNumber (e.g. c:1060, c:550-559, c:281、c:286)"
          value={lineNumber}
          onChange={(e) => setLineNumber(e.target.value)}
        />
        <TextInput
          label="CommitHash (40-char hex, fix commit)"
          value={commitHash}
          onChange={(e) => setCommitHash(e.target.value)}
        />
        <TextInput
          label="CWE (e.g. CWE-787, or 'CWE-125; CWE-787' for multiple)"
          value={cwe}
          onChange={(e) => setCwe(e.target.value)}
        />
        <Textarea
          label="PoC / evidence (optional)"
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
  const [submitOpened, setSubmitOpened] = useState(false)

  return (
    <WithNavBar width="100%" isLoading={!tickets} minWidth={0}>
      <Stack p="md" gap="md">
        <Group justify="space-between">
          <Group>
            <Icon path={mdiBugCheckOutline} size={2} />
            <Title>0day Tickets</Title>
          </Group>
          <Button leftSection={<Icon path={mdiPlus} size={1} />} onClick={() => setSubmitOpened(true)}>
            {t('game.button.zeroday.submit', { defaultValue: 'Submit New Ticket' })}
          </Button>
        </Group>
        {tickets && tickets.length === 0 ? (
          <Center h="40vh">
            <Stack align="center">
              <Title order={3} c="dimmed">
                {t('game.content.zeroday.empty', { defaultValue: 'No 0day tickets yet' })}
              </Title>
              <Text c="dimmed">
                {t('game.content.zeroday.empty_hint', {
                  defaultValue: 'Submit a 0day vulnerability to earn bonus points',
                })}
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
                        <Text fw={700}>{ticket.title || '(no title)'}</Text>
                        <Badge variant="light" color={statusColor[ticket.status ?? ZeroDayTicketStatus.Pending]}>
                          {statusLabel[ticket.status ?? ZeroDayTicketStatus.Pending]}
                        </Badge>
                      </Group>
                      <Text size="sm" lineClamp={3}>
                        {ticket.description}
                      </Text>
                      {ticket.reviewComment && (
                        <Text size="sm" c="dimmed">
                          <strong>Review:</strong> {ticket.reviewComment}
                        </Text>
                      )}
                      {ticket.bonusScore && ticket.bonusScore > 0 && (
                        <Badge variant="outline" color="teal">
                          Bonus: +{ticket.bonusScore}
                        </Badge>
                      )}
                      <Text size="xs" c="dimmed">
                        {ticket.createdAt ? dayjs(ticket.createdAt).format('YYYY-MM-DD HH:mm') : ''}
                        {ticket.reviewedAt ? ` | Reviewed: ${dayjs(ticket.reviewedAt).format('YYYY-MM-DD HH:mm')}` : ''}
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
