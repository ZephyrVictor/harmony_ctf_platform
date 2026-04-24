import {
  Badge,
  Button,
  Card,
  Center,
  Code,
  Grid,
  Group,
  Modal,
  Pagination,
  ScrollArea,
  Stack,
  Table,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { mdiTicketAccount } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useCallback, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { showErrorMsg, showSuccessMsg } from '@Utils/Shared'
import { createTicket, getMySubmissions, getMyTickets, SubmissionRecordDto, TicketDto } from '@Utils/ExtApi'
import { Role } from '@Api'

const statusColor = (status: string) =>
  status === 'Approved' ? 'green' : status === 'Rejected' ? 'red' : 'yellow'

const Submissions: FC = () => {
  const { t } = useTranslation()
  const [records, setRecords] = useState<SubmissionRecordDto[]>([])
  const [tickets, setTickets] = useState<TicketDto[]>([])
  const [loading, setLoading] = useState(true)
  const [ticketModal, ticketModalHandlers] = useDisclosure(false)
  const [detailModal, detailModalHandlers] = useDisclosure(false)
  const [selectedRecord, setSelectedRecord] = useState<SubmissionRecordDto | null>(null)
  const [selectedTicket, setSelectedTicket] = useState<TicketDto | null>(null)
  const [ticketDesc, setTicketDesc] = useState('')
  const [saving, setSaving] = useState(false)
  const [page, setPage] = useState(1)
  const perPage = 20

  const load = useCallback(async () => {
    try {
      const [recs, tix] = await Promise.all([
        getMySubmissions(undefined, 200, 0),
        getMyTickets(),
      ])
      setRecords(recs)
      setTickets(tix)
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setLoading(false)
    }
  }, [t])

  useEffect(() => {
    load()
  }, [load])

  const ticketMap = new Map<number, TicketDto>()
  tickets.forEach((t) => ticketMap.set(t.submissionId, t))

  const openTicket = (record: SubmissionRecordDto) => {
    setSelectedRecord(record)
    setTicketDesc('')
    ticketModalHandlers.open()
  }

  const openDetail = (ticket: TicketDto) => {
    setSelectedTicket(ticket)
    detailModalHandlers.open()
  }

  const handleCreateTicket = async () => {
    if (!selectedRecord || !ticketDesc.trim()) return
    setSaving(true)
    try {
      await createTicket({ submissionId: selectedRecord.submissionId, description: ticketDesc })
      showSuccessMsg(t('common.content.save_success'), t)
      ticketModalHandlers.close()
      await load()
    } catch (err: any) {
      showErrorMsg(err, t)
    } finally {
      setSaving(false)
    }
  }

  const pagedRecords = records.slice((page - 1) * perPage, page * perPage)

  return (
    <WithNavBar width="90%">
      <WithRole requiredRole={Role.User}>
        <Stack gap="md">
          <Title order={4}>{t('game.tab.submissions')}</Title>
          {records.length === 0 && !loading ? (
            <Center h="10rem">
              <Text c="dimmed">{t('game.content.no_submissions')}</Text>
            </Center>
          ) : (
            <>
              <ScrollArea type="never" h="calc(100vh - 20rem)">
                <Table>
                  <Table.Thead>
                    <Table.Tr>
                      <Table.Th>{t('game.content.submission.time')}</Table.Th>
                      <Table.Th>{t('game.content.submission.challenge')}</Table.Th>
                      <Table.Th>{t('game.content.submission.score_change')}</Table.Th>
                      <Table.Th>{t('game.content.submission.ticket')}</Table.Th>
                    </Table.Tr>
                  </Table.Thead>
                  <Table.Tbody>
                    {pagedRecords.map((r) => {
                      const ticket = ticketMap.get(r.submissionId)
                      return (
                        <Table.Tr key={r.id}>
                          <Table.Td>
                            <Text size="sm">{dayjs(r.createdAt).format('L LT')}</Text>
                          </Table.Td>
                          <Table.Td>
                            <Text size="sm">{r.challengeName}</Text>
                          </Table.Td>
                          <Table.Td>
                            <Badge color={r.scoreChange >= 0 ? 'green' : 'red'} variant="light">
                              {r.scoreChange >= 0 ? `+${r.scoreChange}` : r.scoreChange}
                            </Badge>
                          </Table.Td>
                          <Table.Td>
                            {ticket ? (
                              <Badge
                                color={statusColor(ticket.status)}
                                variant="light"
                                style={{ cursor: 'pointer' }}
                                onClick={() => openDetail(ticket)}
                              >
                                {ticket.status}
                              </Badge>
                            ) : (
                              <Button
                                size="xs"
                                variant="light"
                                leftSection={<Icon path={mdiTicketAccount} size={0.7} />}
                                onClick={() => openTicket(r)}
                              >
                                {t('game.button.create_ticket')}
                              </Button>
                            )}
                          </Table.Td>
                        </Table.Tr>
                      )
                    })}
                  </Table.Tbody>
                </Table>
              </ScrollArea>
              <Pagination
                value={page}
                onChange={setPage}
                total={Math.ceil(records.length / perPage)}
              />
            </>
          )}
        </Stack>
      </WithRole>
      <Modal
        opened={ticketModal}
        onClose={ticketModalHandlers.close}
        title={t('game.content.ticket.create_title')}
      >
        <Stack gap="md">
          {selectedRecord && (
            <Card p="sm" withBorder>
              <Group gap="xs">
                <Text size="sm" fw="bold">{selectedRecord.challengeName}</Text>
                <Badge size="xs" color="red" variant="light">{selectedRecord.scoreChange}</Badge>
              </Group>
            </Card>
          )}
          <Textarea
            label={t('game.content.ticket.description')}
            value={ticketDesc}
            onChange={(e) => setTicketDesc(e.currentTarget.value)}
            minRows={3}
            required
          />
          <Group justify="flex-end">
            <Button variant="default" onClick={ticketModalHandlers.close}>
              {t('common.content.cancel')}
            </Button>
            <Button onClick={handleCreateTicket} loading={saving}>
              {t('common.content.save')}
            </Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={detailModal}
        onClose={detailModalHandlers.close}
        title={t('game.content.ticket.detail_title')}
        size="lg"
      >
        {selectedTicket && (
          <Stack gap="md">
            <Grid>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.challenge')}</Text>
                <Text>{selectedTicket.challengeName ?? '—'}</Text>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.status')}</Text>
                <Badge color={statusColor(selectedTicket.status)} variant="light" mt={4}>
                  {selectedTicket.status}
                </Badge>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.answer')}</Text>
                <Code block>{selectedTicket.answer ?? '—'}</Code>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.penalty_score')}</Text>
                <Text fw="bold" c="red">-{selectedTicket.penaltyScore}</Text>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.time')}</Text>
                <Text>{dayjs(selectedTicket.createdAt).format('L LTS')}</Text>
              </Grid.Col>
              <Grid.Col span={6}>
                <Text size="sm" c="dimmed">{t('game.content.ticket.submission_id')}</Text>
                <Text>#{selectedTicket.submissionId}</Text>
              </Grid.Col>
              <Grid.Col span={12}>
                <Text size="sm" c="dimmed">{t('admin.ticket.col.description')}</Text>
                <Text mt={4} style={{ whiteSpace: 'pre-wrap' }}>{selectedTicket.description}</Text>
              </Grid.Col>
            </Grid>

            {selectedTicket.status !== 'Pending' && (
              <Stack gap="xs">
                <Text size="sm" fw="bold">{t('game.content.ticket.review_result')}</Text>
                <Grid>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.reviewer')}</Text>
                    <Text>{selectedTicket.reviewerName ?? '—'}</Text>
                  </Grid.Col>
                  <Grid.Col span={6}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.review_time')}</Text>
                    <Text>
                      {selectedTicket.reviewedAt
                        ? dayjs(selectedTicket.reviewedAt).format('L LTS')
                        : '—'}
                    </Text>
                  </Grid.Col>
                  {selectedTicket.status === 'Approved' && (
                    <>
                      <Grid.Col span={6}>
                        <Text size="sm" c="dimmed">{t('admin.ticket.refund_score')}</Text>
                        <Text c="green">+{selectedTicket.refundScore}</Text>
                      </Grid.Col>
                      <Grid.Col span={6}>
                        <Text size="sm" c="dimmed">{t('admin.ticket.bonus_score')}</Text>
                        <Text c="green">+{selectedTicket.bonusScore}</Text>
                      </Grid.Col>
                    </>
                  )}
                  <Grid.Col span={12}>
                    <Text size="sm" c="dimmed">{t('admin.ticket.comment')}</Text>
                    <Text mt={4}>{selectedTicket.reviewComment || '—'}</Text>
                  </Grid.Col>
                </Grid>
              </Stack>
            )}

            {selectedTicket.status === 'Pending' && (
              <Center h="4rem">
                <Text c="dimmed">{t('game.content.ticket.pending_notice')}</Text>
              </Center>
            )}
          </Stack>
        )}
      </Modal>
    </WithNavBar>
  )
}

export default Submissions
