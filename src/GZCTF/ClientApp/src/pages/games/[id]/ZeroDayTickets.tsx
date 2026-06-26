import {
  Badge,
  Button,
  Card,
  Center,
  Group,
  Modal,
  ScrollArea,
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
import api, { ZeroDayTicketDto, ZeroDayTicketStatus } from '@Api'

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
      })
      showNotification({
        color: 'teal',
        message: t('game.notification.zeroday.submitted', { defaultValue: '0day ticket submitted' }),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      setTitle('')
      setDescription('')
      setChallengeId(undefined)
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
